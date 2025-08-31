using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;

namespace Geex.Tests.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddJob(Job.ShortRun
                .WithRuntime(CoreRuntime.Core90)
                .WithPowerPlan(PowerPlan.UserPowerPlan)
                .WithEvaluateOverhead(false)
                .WithIterationCount(3)     // 减少迭代次数
                .WithWarmupCount(2)        // 减少预热次数
                .WithInvocationCount(96) // 减少调用次数
            ).AddExporter(HtmlExporter.Default)
            ;

        Console.WriteLine("🚀 Geex Reflection Optimization Benchmarks");
        Console.WriteLine("==========================================");

        // 运行所有基准测试
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
