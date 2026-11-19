using System.Linq.Expressions;
using System.Reflection;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Core.Handlers;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.BlobStorage.Tests;

[Collection("BlobStorage")]
public class SpanContainsTests(BlobFixture fixture)
{
    private static Expression Simplify(Expression expression) => (Expression)typeof(DB).Assembly
        .GetType("MongoDB.Entities.InnerQuery.ExpressionSimplifier")!.GetMethod("Simplify", BindingFlags.Public | BindingFlags.Static)!
        .Invoke(null, [null, expression])!;

    private static Expression<Func<T[], T, bool>> Contains<T>(IEqualityComparer<T>? comparer)
    {
        var array = Expression.Parameter(typeof(T[]), "array");
        var item = Expression.Parameter(typeof(T), "item");
        var cast = typeof(ReadOnlySpan<T>).GetMethods().Single(x => x.Name == "op_Implicit" &&
            x.GetParameters() is [{ ParameterType: var type }] && type == typeof(T[]));
        var contains = typeof(MemoryExtensions).GetMethods().Single(x => x.Name == "Contains" &&
            x.IsGenericMethodDefinition && x.GetParameters().Length == 3).MakeGenericMethod(typeof(T));
        return Expression.Lambda<Func<T[], T, bool>>(Expression.Call(contains, Expression.Call(cast, array), item,
            Expression.Constant(comparer, typeof(IEqualityComparer<T>))), array, item);
    }

    [Fact]
    public void NullComparerPreservesSmartEnumEquality()
    {
        var expression = (Expression<Func<BlobStorageType[], BlobStorageType, bool>>)Simplify(Contains<BlobStorageType>(null));
        Assert.Equal(typeof(Enumerable), ((MethodCallExpression)expression.Body).Method.DeclaringType);
        var predicate = expression.Compile();
        Assert.True(predicate([BlobStorageType.Db], BlobStorageType.Db));
        Assert.False(predicate([BlobStorageType.Db], BlobStorageType.Cache));
    }

    [Fact]
    public void ExplicitComparerIsNotDropped()
    {
        var expression = (LambdaExpression)Simplify(Contains<string>(StringComparer.OrdinalIgnoreCase));
        var call = Assert.IsAssignableFrom<MethodCallExpression>(expression.Body);
        Assert.Equal(typeof(MemoryExtensions), call.Method.DeclaringType);
        Assert.Same(StringComparer.OrdinalIgnoreCase, Assert.IsAssignableFrom<ConstantExpression>(call.Arguments[2]).Value);
    }

    [Fact]
    public async Task SmartEnumArrayContainsTranslatesThroughRealMongoQuery()
    {
        using var work = new GeexDbContext(fixture.Services);
        var blob = await new BlobObjectHandler(work).Handle(new CreateBlobObjectRequest
        { File = new StreamFile(Guid.NewGuid() + ".txt", () => new MemoryStream([1, 2, 3])), StorageType = BlobStorageType.FileSystem },
            CancellationToken.None);
        await work.SaveChanges();
        BlobStorageType[] types = [BlobStorageType.FileSystem];
        var found = work.Query<BlobObject>().Where(x => types.Contains(x.StorageType) && x.Id == blob.Id).Single();
        Assert.Equal(blob.Id, found.Id);
        Assert.Empty(work.Query<BlobObject>().Where(x => new[] { BlobStorageType.Db }.Contains(x.StorageType) && x.Id == blob.Id));
    }
}
