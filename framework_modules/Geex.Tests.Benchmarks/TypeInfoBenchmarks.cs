using BenchmarkDotNet.Attributes;
using System.Reflection;
using Geex.MongoDB.Entities.Utilities;
using MongoDB.Entities.Utilities;

namespace Geex.Tests.Benchmarks;

/// <summary>
/// 类型信息获取性能基准测试
/// </summary>
[MemoryDiagnoser]
public class TypeInfoBenchmarks
{

    #region 泛型方法信息获取

    [Benchmark]
    public MethodInfo GenericMethodInfo_Traditional()
    {
        var method = typeof(TestModel).GetMethod(nameof(TestModel.GenericMethod))!;
        return method.MakeGenericMethod(typeof(string));
    }

    [Benchmark]
    public MethodInfo GenericMethodInfo_Optimized()
    {
        var method = typeof(TestModel).GetMethod(nameof(TestModel.GenericMethod))!;
        return method.MakeGenericMethodFast(typeof(string));
    }

    #endregion
}
