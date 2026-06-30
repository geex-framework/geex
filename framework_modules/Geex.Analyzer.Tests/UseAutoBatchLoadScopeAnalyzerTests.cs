using System.Threading.Tasks;

using Geex.Analyzer.Analyzer;
using Geex.Analyzer.Tests;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<UseAutoBatchLoadScopeAnalyzer>;

    public class UseAutoBatchLoadScopeAnalyzerTests
    {
        [Fact]
        public async Task ValidFieldLevelUseAutoBatchLoad_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync(
                "AutoBatchLoadTests/ValidFieldLevelUseAutoBatchLoad.cs");
        }

        [Fact]
        public async Task ValidOperationLevelUseAutoBatchLoad_ShouldNotReportDiagnostic()
        {
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync(
                "AutoBatchLoadTests/ValidOperationLevelUseAutoBatchLoad.cs");
        }

        [Fact]
        public async Task InvalidEntityFieldLevelUseAutoBatchLoad_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder.Create("GEEX006");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/InvalidEntityFieldLevelUseAutoBatchLoad.cs",
                expected);
        }

        [Fact]
        public async Task InvalidEntityTypeLevelUseAutoBatchLoad_ShouldReportDiagnostic()
        {
            var expected = DiagnosticResultBuilder.Create("GEEX006");

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/InvalidEntityTypeLevelUseAutoBatchLoad.cs",
                expected);
        }
    }
}
