using BenchmarkDotNet.Attributes;

using System.Reflection;
using MongoDB.Entities.Utilities;

namespace Geex.Tests.Benchmarks;

/// <summary>
/// 方法调用性能基准测试
/// </summary>
[MemoryDiagnoser]
public class MethodInvocationBenchmarks
{
    private TestModel _testInstance = new();
    private MethodInfo _instanceMethod = null!;
    private MethodInfo _staticMethod = null!;
    private MethodInfo _genericMethod = null!;

    [GlobalSetup]
    public void Setup()
    {
        _instanceMethod = typeof(TestModel).GetMethod(nameof(TestModel.UpdateValue))!;
        _staticMethod = typeof(TestModel).GetMethod(nameof(TestModel.FormatValue))!;
        _genericMethod = typeof(TestModel).GetMethod(nameof(TestModel.GenericMethod))!;
    }


    #region 泛型方法调用

    [Benchmark]
    public void GenericMethod_Traditional()
    {
        var genericMethod = _genericMethod.MakeGenericMethod(typeof(string));
        genericMethod.Invoke(null, new object[] { "GenericTest" });

    }

    [Benchmark]
    public void GenericMethod_Optimized()
    {
        var genericMethod = _genericMethod.MakeGenericMethodFast(typeof(string));
        genericMethod.Invoke(null, new object[] { "GenericTest" });
    }

    #endregion

    #region 重复调用测试 - 验证缓存效果

    [Benchmark]
    public void GenericMethod_Traditional_Repeated()
    {
        var genericMethod = _genericMethod.MakeGenericMethod(typeof(int));
        for (int i = 0; i < 100; i++)
        {
            genericMethod.Invoke(null, new object[] { i });
        }
    }

    [Benchmark]
    public void GenericMethod_Optimized_Repeated()
    {
        var genericMethod = _genericMethod.MakeGenericMethodFast(typeof(int));
        for (int i = 0; i < 100; i++)
        {
            genericMethod.Invoke(null, new object[] { i });
        }
    }

    #endregion
}
