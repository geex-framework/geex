using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<AutoBatchLoadDependsOnAnalyzer>;

    public class AutoBatchLoadDependsOnAnalyzerTests
    {
        [Fact]
        public async Task MissingAutoBatchLoadDependsOn_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/MissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task ValidAutoBatchLoadDependsOn_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/ValidDependsOn.cs");
        }

        [Fact]
        public async Task NonEntityType_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/NonEntitySummary.cs");
        }

        [Fact]
        public async Task MissingMultipleDependsOn_ShouldReportMultipleDiagnostics()
        {
            var expected = new[]
            {
                DiagnosticResultBuilder
                    .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("Summary", "ArchivedLines"),
                DiagnosticResultBuilder
                    .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("Summary", "Lines"),
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/MissingMultipleDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task InheritedLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/InheritedMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task BaseAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/BaseAccessMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task ConditionalAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("LineCount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/ConditionalAccessMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task PrivateLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/PrivateLazyNavMissingDependsOn.cs",
                expected);
        }

        [Fact]
        public async Task PrivateMethodAccessLazyNavigation_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder
                .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                .WithArguments("TotalAmount", "Lines");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/AutoBatchLoadDependsOnTests/PrivateMethodMissingDependsOn.cs",
                expected);
        }
    }
}
