using System;
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
  public class AutoBatchLoadMiddleware
  {
    public const string MiddlewareKey = "Geex.AutoBatchLoad.Middleware";

    private readonly FieldDelegate _next;

    public AutoBatchLoadMiddleware(FieldDelegate next)
    {
      _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
      if (context.Selection.Field.IsSystemOrIntrospectionField())
      {
        await _next(context).ConfigureAwait(false);
        return;
      }

      if (!context.Selection.Field.TryGetReturningEntityType(out var entityType))
      {
        await _next(context).ConfigureAwait(false);
        return;
      }

      var selectionConfig = SelectionTreeWalker.Analyze(context, entityType);

      await _next(context).ConfigureAwait(false);

      TryApplySelectionBatchLoad(context, selectionConfig);
    }

    private static void TryApplySelectionBatchLoad(IMiddlewareContext context, BatchLoadConfig selectionConfig)
    {
      if (selectionConfig.SubBatchLoadConfigs.Count == 0)
      {
        return;
      }

      if (context.Result is IQueryable queryable)
      {
        ApplyToQueryable(context, queryable, selectionConfig);
        return;
      }

      if (BatchLoadObservableAdapter.TryWrap(context.Result, selectionConfig, out var wrapped))
      {
        context.Result = wrapped;
      }
    }

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
            context.Selection.SyntaxNode.ToString()
            );
        return;
      }

      provider.BatchLoadConfig.ApplySelectionBatchLoad(selectionConfig);
    }
  }

  internal static class BatchLoadObservableAdapter
  {
    private static readonly MethodInfo WrapTypedMethod = typeof(BatchLoadObservableAdapter)
        .GetMethod(nameof(WrapTyped), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static bool TryWrap(object? result, BatchLoadConfig config, out object wrapped)
    {
      wrapped = result!;
      if (result == null)
      {
        return false;
      }

      if (!TryGetObservableElementType(result.GetType(), out var elementType))
      {
        return false;
      }

      wrapped = WrapTypedMethod
          .MakeGenericMethodFast(elementType)
          .Invoke(null, [result, config])!;
      return true;
    }

    private static IObservable<T> WrapTyped<T>(IObservable<T> source, BatchLoadConfig config) =>
        new BatchLoadObservable<T>(source, config);

    private static bool TryGetObservableElementType(Type type, out Type elementType)
    {
      elementType = null!;
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
      {
        elementType = type.GetGenericArguments()[0];
        return true;
      }

      var observableInterface = type.GetInterfaces()
          .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IObservable<>));
      if (observableInterface != null)
      {
        elementType = observableInterface.GetGenericArguments()[0];
        return true;
      }

      return false;
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
        _inner.Subscribe(new BatchLoadObserver<T>(observer, _config));
  }

  internal sealed class BatchLoadObserver<T> : IObserver<T>
  {
    private readonly IObserver<T> _inner;
    private readonly BatchLoadConfig _config;

    public BatchLoadObserver(IObserver<T> inner, BatchLoadConfig config)
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
