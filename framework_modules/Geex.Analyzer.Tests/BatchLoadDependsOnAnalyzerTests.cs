using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<BatchLoadDependsOnAnalyzer>;

    public class BatchLoadDependsOnAnalyzerTests
    {
        [Fact]
        public async Task MissingBatchLoadDependsOn_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/MissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task ValidBatchLoadDependsOn_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/ValidDependsOn.cs");
        }

        [Fact]
        public async Task NonEntityType_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/NonEntitySummary.cs");
        }

        [Fact]
        public async Task MissingMultipleDependsOn_ShouldReportMultipleDiagnostics()
        {
            var expected = new[]
            {
                DiagnosticResultBuilder
                    .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("Summary", "ArchivedLines"),
                DiagnosticResultBuilder
                    .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("Summary", "Lines"),
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/MissingMultipleDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task InheritedLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/InheritedMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task BaseAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/BaseAccessMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task ConditionalAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("LineCount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/ConditionalAccessMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task PrivateLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/PrivateLazyNavMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task PrivateMethodAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/BatchLoadDependsOnTests/PrivateMethodMissingDependsOn.cs",
                expected);
        }
    }
}
