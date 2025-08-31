using BenchmarkDotNet.Attributes;

using System.Reflection;

namespace Geex.Tests.Benchmarks;

/// <summary>
/// 枚举构造性能基准测试
/// </summary>
[MemoryDiagnoser]
public class EnumerationBenchmarks
{
    private readonly string[] _testValues = { "Dynamic1", "Dynamic2", "Dynamic3", "Dynamic4", "Dynamic5" };

    #region 单个枚举创建

    [Benchmark(Baseline = true)]
    public TestEnumeration EnumerationCreation_Traditional()
    {
        // 模拟传统方式：使用 Activator.CreateInstance
        var instance = (TestEnumeration)Activator.CreateInstance(typeof(TestEnumeration))!;
        return instance;
    }

    [Benchmark]
    public TestEnumeration EnumerationCreation_Optimized()
    {
        // 使用优化后的 FromValue 方法（内部使用了高性能构造函数缓存）
        return TestEnumeration.FromValue("OptimizedTest");
    }

    #endregion

    #region FromValue 方法对比

    [Benchmark]
    public TestEnumeration FromValue_ExistingValue()
    {
        // 测试已存在值的查找性能
        return TestEnumeration.FromValue("Active");
    }

    [Benchmark]
    public TestEnumeration FromValue_NewValue()
    {
        // 测试动态创建新值的性能（内部使用了优化的构造函数）
        return TestEnumeration.FromValue("DynamicValue");
    }

    #endregion

    #region 批量创建测试

    [Benchmark]
    public void BatchCreation_Traditional()
    {
        for (int j = 0; j < 100; j++)
        {
            for (int i = 0; i < _testValues.Length; i++)
            {
                var instance = (StatusEnumeration)Activator.CreateInstance(typeof(StatusEnumeration))!;
                // 模拟设置值的过程（简化）
            }
        }
    }

    [Benchmark]
    public void BatchCreation_Optimized()
    {
        for (int j = 0; j < 100; j++)
        {
            for (int i = 0; i < _testValues.Length; i++)
            {
                StatusEnumeration.FromValue(_testValues[i]);
            }
        }
    }

    #endregion

    #region 缓存效果测试

    [Benchmark]
    public void RepeatedAccess_SameValue()
    {
        // 测试重复访问同一个值的缓存效果
        for (int i = 0; i < 100; i++)
        {
            TestEnumeration.FromValue("CachedValue");
        }
    }

    [Benchmark]
    public void RepeatedAccess_DifferentValues()
    {
        // 测试访问不同值时的缓存效果
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < _testValues.Length; j++)
            {
                TestEnumeration.FromValue(_testValues[j]);
            }
        }
    }

    #endregion

    #region 类型化FromValue测试

    [Benchmark]
    public TestEnumeration TypedFromValue()
    {
        return TestEnumeration.FromValue<TestEnumeration>("TypedTest");
    }

    [Benchmark]
    public StatusEnumeration DifferentTypeFromValue()
    {
        return StatusEnumeration.FromValue<StatusEnumeration>("StatusTest");
    }

    #endregion
}
