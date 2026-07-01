using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        private static readonly ConcurrentDictionary<Type, Func<object, BatchLoadConfig, object>> ObservableWrapCache =
            new();

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

            context.Result = ObservableWrapCache
                .GetOrAdd(observablePayloadType, CreateObservableWrapFactory)(result, selectionConfig);
        }

        private static Func<object, BatchLoadConfig, object> CreateObservableWrapFactory(Type payloadType)
        {
            var method = WrapObservableMethod.MakeGenericMethodFast(payloadType);
            return (obj, cfg) => method.Invoke(null, [obj, cfg])!;
        }

        private static IObservable<T> WrapObservable<T>(IObservable<T> source, BatchLoadConfig config) =>
            new BatchLoadObservable<T>(source, config);

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

    internal sealed class BatchLoadObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> _inner;
        private readonly BatchLoadConfig _config;

        public BatchLoadObservable(IObservable<T> inner, BatchLoadConfig config)
        {
            _inner = inner;
            _config = config;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            _inner.Subscribe(new Observer(observer, _config));

        private sealed class Observer : IObserver<T>
        {
            private readonly IObserver<T> _inner;
            private readonly BatchLoadConfig _config;

            public Observer(IObserver<T> inner, BatchLoadConfig config)
            {
                _inner = inner;
                _config = config;
            }

            public void OnNext(T value)
            {
                if (value is IQueryable queryable &&
                    queryable.Provider is ICachedDbContextQueryProvider provider)
                {
                    provider.BatchLoadConfig.ApplySelectionBatchLoad(_config);
                }

                _inner.OnNext(value);
            }

            public void OnError(Exception error) => _inner.OnError(error);

            public void OnCompleted() => _inner.OnCompleted();
        }
    }
}
