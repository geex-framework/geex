using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;
using Xunit;

namespace Geex.Tests.UnitTests;

public class DataFilterScopeTests
{
    [Fact]
    public void DisableDataFilters_OnlyDisablesRequestedFilterAndRestoresSameInstance()
    {
        var first = CreateFilter<IFirst>();
        var unrelated = CreateFilter<IUnrelated>();
        var context = CreateContext((typeof(IFirst), first), (typeof(IUnrelated), unrelated));

        using (context.DisableDataFilters(typeof(IFirst)))
        {
            Assert.False(context.DataFilters.ContainsKey(typeof(IFirst)));
            Assert.Same(unrelated, context.DataFilters[typeof(IUnrelated)]);
        }

        Assert.Equal(2, context.DataFilters.Count);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
        Assert.Same(unrelated, context.DataFilters[typeof(IUnrelated)]);
    }

    [Fact]
    public void DisableDataFilters_MissingFilterRemainsAbsentAfterScope()
    {
        var first = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));

        using (context.DisableDataFilters(typeof(IMissing)))
        {
            Assert.False(context.DataFilters.ContainsKey(typeof(IMissing)));
        }

        Assert.False(context.DataFilters.ContainsKey(typeof(IMissing)));
        Assert.Single(context.DataFilters);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
    }

    [Fact]
    public void DisableDataFilters_NestedSameFilterRemainsDisabledUntilOuterScopeEnds()
    {
        var first = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));

        using (context.DisableDataFilters(typeof(IFirst)))
        {
            using (context.DisableDataFilters(typeof(IFirst)))
            {
                Assert.Empty(context.DataFilters);
            }

            Assert.Empty(context.DataFilters);
        }

        Assert.Single(context.DataFilters);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
    }

    [Fact]
    public void DisableDataFilters_PartiallyOverlappingScopesRestoreOnlyTheirOwnFilters()
    {
        var first = CreateFilter<IFirst>();
        var second = CreateFilter<ISecond>();
        var unrelated = CreateFilter<IUnrelated>();
        var context = CreateContext(
            (typeof(IFirst), first), (typeof(ISecond), second), (typeof(IUnrelated), unrelated));

        using (context.DisableDataFilters(typeof(IFirst)))
        {
            using (context.DisableDataFilters(typeof(IFirst), typeof(ISecond), typeof(IMissing)))
            {
                Assert.Single(context.DataFilters);
                Assert.Same(unrelated, context.DataFilters[typeof(IUnrelated)]);
            }

            Assert.False(context.DataFilters.ContainsKey(typeof(IFirst)));
            Assert.False(context.DataFilters.ContainsKey(typeof(IMissing)));
            Assert.Same(second, context.DataFilters[typeof(ISecond)]);
            Assert.Same(unrelated, context.DataFilters[typeof(IUnrelated)]);
        }

        Assert.Equal(3, context.DataFilters.Count);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
        Assert.Same(second, context.DataFilters[typeof(ISecond)]);
        Assert.Same(unrelated, context.DataFilters[typeof(IUnrelated)]);
    }

    [Fact]
    public void DisableDataFilters_DuplicateArgumentsRestoreOriginalFilterOnce()
    {
        var first = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));

        using (context.DisableDataFilters(typeof(IFirst), typeof(IFirst)))
        {
            Assert.Empty(context.DataFilters);
        }

        Assert.Single(context.DataFilters);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
    }

    [Fact]
    public void DisableDataFilters_ExceptionUnwindingRestoresOriginalFilter()
    {
        var first = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));

        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using var scope = context.DisableDataFilters(typeof(IFirst));
            Assert.Empty(context.DataFilters);
            throw new InvalidOperationException("Scope failed.");
        }));

        Assert.Single(context.DataFilters);
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
    }

    [Fact]
    public void DisableDataFilters_NullEntryIsNotRestored()
    {
        var context = CreateContext((typeof(IFirst), null!));

        using (context.DisableDataFilters(typeof(IFirst)))
        {
            Assert.Empty(context.DataFilters);
        }

        Assert.Empty(context.DataFilters);
    }

    [Fact]
    public void Dispose_RepeatedCallDoesNotRestoreFilterRemovedAfterFirstCall()
    {
        var first = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));
        var scope = context.DisableDataFilters(typeof(IFirst));

        scope.Dispose();
        Assert.Same(first, context.DataFilters[typeof(IFirst)]);
        Assert.True(context.DataFilters.TryRemove(typeof(IFirst), out _));

        scope.Dispose();

        Assert.Empty(context.DataFilters);
    }

    [Fact]
    public void Dispose_DoesNotOverwriteFilterRegisteredWhileDisabled()
    {
        var first = CreateFilter<IFirst>();
        var replacement = CreateFilter<IFirst>();
        var context = CreateContext((typeof(IFirst), first));

        using (context.DisableDataFilters(typeof(IFirst)))
        {
            Assert.True(context.DataFilters.TryAdd(typeof(IFirst), replacement));
        }

        Assert.Single(context.DataFilters);
        Assert.Same(replacement, context.DataFilters[typeof(IFirst)]);
    }

    private static DbContext CreateContext(params (Type Type, IDataFilter Filter)[] filters)
    {
        // Bypass the constructor's MongoDB session creation; these tests only use the filter dictionary.
        var context = (DbContext)RuntimeHelpers.GetUninitializedObject(typeof(DbContext));
        var values = new ConcurrentDictionary<Type, IDataFilter>(
            filters.Select(x => new KeyValuePair<Type, IDataFilter>(x.Type, x.Filter)));
        typeof(DbContext)
            .GetField("_dataFilters", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(context, values);
        return context;
    }

    private static IDataFilter CreateFilter<T>() => new ExpressionDataFilter<T>(null!, null!);

    private interface IFirst { }
    private interface ISecond { }
    private interface IUnrelated { }
    private interface IMissing { }
}
