using System.Threading.Tasks;
using Geex.Analyzer.Analyzers;
using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<BannedMediatRAnalyzer>;

    /// <summary>
    /// BannedMediatRAnalyzer 的测试类
    /// 使用 Geex.Analyzer.TestCode 项目中的真实代码进行测试
    /// </summary>
    public class BannedMediatRAnalyzerTests
    {
        [Fact(Skip = "todo")]
        public async Task UsingMediatRNamespace_ShouldReportDiagnostic()
        {
            // 测试使用 MediatR 命名空间应该报告 GEEX001 诊断
            var expected = DiagnosticResultBuilder.Create("GEEX001")
                .WithSpan(4, 1, 4, 16) // using MediatR; 的位置
                .WithArguments("MediatR");

            await AnalyzerVerifier.VerifyAnalyzerAsync("MediatRTests/UsingMediatRNamespace.cs", expected);
        }

        [Fact(Skip = "todo")]
        public async Task UsingBannedMediatRTypes_ShouldReportDiagnostics()
        {
            // 测试使用被禁止的 MediatR 类型应该报告 GEEX002 诊断
            var expectedDiagnostics = new[]
            {
                // IMediator 字段声明
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IMediator", "MediatX.IMediator"),

                // IMediator 构造函数参数
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IMediator", "MediatX.IMediator"),

                // IRequest 接口实现
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IRequest", "MediatX.IRequest"),

                // IRequestHandler 接口实现
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IRequestHandler", "MediatX.IRequestHandler"),

                // INotification 接口实现
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.INotification", "MediatX.IEvent"),

                // INotificationHandler 接口实现
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.INotificationHandler", "MediatX.IEventHandlerr")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("MediatRTests/UsingBannedMediatRTypes.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task UsingMediatXTypes_ShouldNotReportDiagnostics()
        {
            // 测试正确使用 MediatX 命名空间和类型不应该报告任何诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("MediatRTests/UsingMediatXTypes.cs");
        }

        [Fact]
        public async Task EmptyFile_ShouldNotReportDiagnostics()
        {
            // 测试空文件或不包含 MediatR 相关代码的文件不应该报告诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("MediatRTests/EmptyFile.cs");
        }

        [Fact(Skip = "todo")]
        public async Task ComplexMediatRUsage_ShouldReportMultipleDiagnostics()
        {
            // 测试复杂的 MediatR 使用场景应该报告多个诊断
            var expectedDiagnostics = new[]
            {
                // using MediatR; 语句
                DiagnosticResultBuilder.Create("GEEX001")
                    .WithArguments("MediatR"),

                // 各种 MediatR 类型的使用
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IMediator", "MediatX.IMediator"),
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IRequest", "MediatX.IRequest"),
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.IRequestHandler", "MediatX.IRequestHandler"),
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.INotification", "MediatX.IEvent"),
                DiagnosticResultBuilder.Create("GEEX002")
                    .WithArguments("MediatR.INotificationHandler", "MediatX.IEventHandlerr")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("MediatRTests/ComplexMediatRUsage.cs", expectedDiagnostics);
        }
    }
}
