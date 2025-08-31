using BenchmarkDotNet.Attributes;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Utilities;

namespace Geex.Tests.Benchmarks;

/// <summary>
/// 整体性能对比基准测试 - 模拟真实应用场景
/// </summary>
[MemoryDiagnoser]
public class OverallComparisonBenchmarks
{
    private MethodInfo _genericMethod = null!;

    [GlobalSetup]
    public void Setup()
    {
        _genericMethod = typeof(TestModel).GetMethod(nameof(TestModel.GenericMethod))!;

        // BSON 设置
        if (!BsonClassMap.IsClassMapRegistered(typeof(BsonTestModel)))
        {
            BsonClassMap.RegisterClassMap<BsonTestModel>(cm => cm.AutoMap());
        }
    }

    #region 高频调用场景

    [Benchmark]
    public void HighFrequency_Traditional()
    {
        // 模拟高频调用场景
        var genericMethod = _genericMethod.MakeGenericMethod(typeof(int));
        for (int i = 0; i < 50; i++)
        {
            genericMethod.Invoke(null, new object[] { i });
            TestEnumeration.FromValue("HighFreq");
        }
    }

    [Benchmark]
    public void HighFrequency_Optimized()
    {
        // 优化版本 - 应该显示出明显的缓存效果
        var genericMethod = _genericMethod.MakeGenericMethodFast(typeof(int));
        for (int i = 0; i < 50; i++)
        {
            genericMethod.Invoke(null, new object[] { i });
            TestEnumeration.FromValue("HighFreq");
        }
    }

    #endregion

    #region 内存压力测试

    [Benchmark]
    public void MemoryPressure_Traditional()
    {
        // 测试内存分配和GC压力
        var results = new object[20];
        var genericMethod = _genericMethod.MakeGenericMethod(typeof(string));
        for (int i = 0; i < 20; i++)
        {
            results[i] = genericMethod.Invoke(null, new object[] { $"Memory{i}" });
        }
    }

    [Benchmark]
    public void MemoryPressure_Optimized()
    {
        // 优化版本应该显示更少的内存分配
        var genericMethod = _genericMethod.MakeGenericMethodFast(typeof(string));
        var results = new object[20];
        for (int i = 0; i < 20; i++)
        {
            results[i] = genericMethod.Invoke(null, new object[] { $"Memory{i}" });
        }
    }

    #endregion

    #region 冷启动性能测试

    [Benchmark]
    public void ColdStart_Traditional()
    {
        // 模拟应用冷启动时的反射调用
        var type = typeof(TestModel);
        var method = type.GetMethod(nameof(TestModel.GenericMethod))!;

        var genericMethod = method.MakeGenericMethod(typeof(string));
        genericMethod.Invoke(null, new object[] { "ColdStart" });
    }

    [Benchmark]
    public void ColdStart_Optimized()
    {
        // 优化版本的冷启动性能
        var type = typeof(TestModel);
        var method = type.GetMethod(nameof(TestModel.GenericMethod))!;
        var genericMethod = method.MakeGenericMethodFast(typeof(string));
        genericMethod.Invoke(null, new object[] { "ColdStart" });
    }

    #endregion
}
