using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Geex.Gql;
using HotChocolate.Resolvers;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    internal sealed class AutoBatchLoadMiddleware
    {
        private static readonly MethodInfo WrapObservableMethod = typeof(AutoBatchLoadMiddleware)
            .GetMethod(nameof(WrapObservable), BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly ConcurrentDictionary<Type, Func<object, BatchLoadConfig, AutoBatchLoadObservableWarningContext, object>>
            ObservableWrapCache = new();

        public AutoBatchLoadMiddleware() { }

        public async Task InvokeAsync(IMiddlewareContext context, FieldDelegate next)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (!IsAutoBatchLoadEnabled(context))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (context.Selection.Field.IsSystemOrIntrospectionField())
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (!context.Selection.Field.TryGetReturningEntityType(out var entityType))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var isObservableField = context.Selection.Field.TryGetObservablePayloadType(out var observablePayloadType);

            await next(context).ConfigureAwait(false);

            var result = context.Result;
            if (result is IQueryable queryable)
            {
                TryApplySelectionBatchLoadToQueryable(context, queryable, entityType);
                return;
            }

            if (isObservableField && result != null)
            {
                TryApplySelectionBatchLoadToObservable(context, entityType, observablePayloadType, result);
            }
        }

        private static bool IsAutoBatchLoadEnabled(IMiddlewareContext context) =>
            context.Selection.Field.GeexFeatures.AutoBatchLoad?.IsEnabled == true;

        private static void TryApplySelectionBatchLoadToQueryable(
            IMiddlewareContext context,
            IQueryable queryable,
            Type entityType)
        {
            var selectionConfig = SelectionTreeWalker.Analyze(context, entityType);
            if (selectionConfig.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            ApplyToQueryable(context, queryable, selectionConfig);
        }

        private static void TryApplySelectionBatchLoadToObservable(
            IMiddlewareContext context,
            Type entityType,
            Type observablePayloadType,
            object result)
        {
            var selectionConfig = SelectionTreeWalker.Analyze(context, entityType);
            if (selectionConfig.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            if (!TryWrapObservableResult(context, observablePayloadType, result, selectionConfig, out var wrapped))
            {
                return;
            }

            context.Result = wrapped;
        }

        private static bool TryWrapObservableResult(
            IMiddlewareContext context,
            Type declaredPayloadType,
            object result,
            BatchLoadConfig selectionConfig,
            out object? wrapped)
        {
            wrapped = null;
            var logger = context.Services.GetService<ILogger<AutoBatchLoadMiddleware>>();
            var fieldName = context.Selection.Field.Name;
            var syntaxNode = context.Selection.SyntaxNode.ToString();

            if (GetObservablePayloadType(result.GetType()) == null)
            {
                logger?.LogWarning(
                    "AutoBatchLoad 未能应用到字段 {FieldName}：resolver 返回值不是 IObservable<>，" +
                    "实际类型为 {ResultType}。此处可能发生 N+1 查询。\n {SyntaxNode}",
                    fieldName,
                    result.GetType().Name,
                    syntaxNode);
                return false;
            }

            var actualPayloadType = GetObservablePayloadType(result.GetType())!;

            if (!declaredPayloadType.IsAssignableFrom(actualPayloadType) &&
                !actualPayloadType.IsAssignableFrom(declaredPayloadType))
            {
                logger?.LogWarning(
                    "AutoBatchLoad 未能应用到字段 {FieldName}：IObservable payload 类型 {ActualPayloadType} " +
                    "与字段声明类型 {DeclaredPayloadType} 不兼容。此处可能发生 N+1 查询。\n {SyntaxNode}",
                    fieldName,
                    actualPayloadType.Name,
                    declaredPayloadType.Name,
                    syntaxNode);
                return false;
            }

            var warningContext = new AutoBatchLoadObservableWarningContext(logger, fieldName, syntaxNode);
            wrapped = ObservableWrapCache
                .GetOrAdd(actualPayloadType, CreateObservableWrapFactory)(result, selectionConfig, warningContext);
            return true;
        }

        private static Type? GetObservablePayloadType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static Func<object, BatchLoadConfig, AutoBatchLoadObservableWarningContext, object> CreateObservableWrapFactory(
            Type payloadType)
        {
            var method = WrapObservableMethod.MakeGenericMethodFast(payloadType);
            return (obj, cfg, warningContext) => method.Invoke(null, [obj, cfg, warningContext])!;
        }

        private static IObservable<T> WrapObservable<T>(
            IObservable<T> source,
            BatchLoadConfig config,
            AutoBatchLoadObservableWarningContext warningContext) =>
            new BatchLoadObservable<T>(source, config, warningContext);

        private static void ApplyToQueryable(
            IMiddlewareContext context,
            IQueryable queryable,
            BatchLoadConfig selectionConfig)
        {
            if (queryable.Provider is not ICachedDbContextQueryProvider provider)
            {
                context.Services.GetService<ILogger<AutoBatchLoadMiddleware>>()?.LogWarning(
                    "AutoBatchLoad 未能应用到字段 {FieldName}：结果 IQueryable 的 Provider 类型为 {ProviderType}，" +
                    "而非 {ExpectedProviderType}。此处可能发生 N+1 查询。\n {SyntaxNode}",
                    context.Selection.Field.Name,
                    queryable.Provider.GetType().Name,
                    nameof(ICachedDbContextQueryProvider),
                    context.Selection.SyntaxNode.ToString());
                return;
            }

            provider.BatchLoadConfig.ApplySelectionBatchLoad(selectionConfig);
        }
    }

    internal sealed class AutoBatchLoadObservableWarningContext
    {
        private readonly ILogger? _logger;
        private readonly string _fieldName;
        private readonly string _syntaxNode;
        private int _providerMismatchWarned;
        private int _nonQueryablePayloadWarned;

        public AutoBatchLoadObservableWarningContext(ILogger? logger, string fieldName, string syntaxNode)
        {
            _logger = logger;
            _fieldName = fieldName;
            _syntaxNode = syntaxNode;
        }

        public void WarnProviderMismatch(string providerType)
        {
            if (Interlocked.CompareExchange(ref _providerMismatchWarned, 1, 0) != 0)
            {
                return;
            }

            _logger?.LogWarning(
                "AutoBatchLoad 未能应用到字段 {FieldName} 的 Observable 发射值：IQueryable Provider 类型为 {ProviderType}，" +
                "而非 {ExpectedProviderType}。此处可能发生 N+1 查询。\n {SyntaxNode}",
                _fieldName,
                providerType,
                nameof(ICachedDbContextQueryProvider),
                _syntaxNode);
        }

        public void WarnNonQueryablePayload(string payloadType)
        {
            if (Interlocked.CompareExchange(ref _nonQueryablePayloadWarned, 1, 0) != 0)
            {
                return;
            }

            _logger?.LogWarning(
                "AutoBatchLoad 未能应用到字段 {FieldName} 的 Observable 发射值：payload 类型为 {PayloadType}，" +
                "而非 IQueryable。此处可能发生 N+1 查询。\n {SyntaxNode}",
                _fieldName,
                payloadType,
                _syntaxNode);
        }
    }

    internal sealed class BatchLoadObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> _inner;
        private readonly BatchLoadConfig _config;
        private readonly AutoBatchLoadObservableWarningContext _warningContext;

        public BatchLoadObservable(
            IObservable<T> inner,
            BatchLoadConfig config,
            AutoBatchLoadObservableWarningContext warningContext)
        {
            _inner = inner;
            _config = config;
            _warningContext = warningContext;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            _inner.Subscribe(new Observer(observer, _config, _warningContext));

        private sealed class Observer : IObserver<T>
        {
            private readonly IObserver<T> _inner;
            private readonly BatchLoadConfig _config;
            private readonly AutoBatchLoadObservableWarningContext _warningContext;

            public Observer(
                IObserver<T> inner,
                BatchLoadConfig config,
                AutoBatchLoadObservableWarningContext warningContext)
            {
                _inner = inner;
                _config = config;
                _warningContext = warningContext;
            }

            public void OnNext(T value)
            {
                if (value is IQueryable queryable)
                {
                    if (queryable.Provider is ICachedDbContextQueryProvider provider)
                    {
                        provider.BatchLoadConfig.ApplySelectionBatchLoad(_config);
                    }
                    else
                    {
                        _warningContext.WarnProviderMismatch(queryable.Provider.GetType().Name);
                    }
                }
                else if (value != null)
                {
                    _warningContext.WarnNonQueryablePayload(value.GetType().Name);
                }

                _inner.OnNext(value);
            }

            public void OnError(Exception error) => _inner.OnError(error);

            public void OnCompleted() => _inner.OnCompleted();
        }
    }
}
