using System.Threading.Tasks;

using Geex.Analyzer.Analyzer;

using Microsoft.CodeAnalysis;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<UseAutoBatchLoadScopeAnalyzer>;

    public class UseAutoBatchLoadScopeAnalyzerTests
    {
        [Fact]
        public async Task FieldLevel_UseAutoBatchLoad_ShouldReportError()
        {
            var expected = DiagnosticResultBuilder
                .CreateError("GEEX006")
                .WithSpan(15, 13, 15, 42);

            await AnalyzerVerifier.VerifyAnalyzerAsync(
                "AutoBatchLoadTests/InvalidFieldLevelUseAutoBatchLoad.cs",
                includeTestCodeAssembly: false,
                expected);
        }

        [Fact]
        public async Task OperationLevel_UseAutoBatchLoad_ShouldNotReport()
        {
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync(
                "AutoBatchLoadTests/ValidOperationLevelUseAutoBatchLoad.cs");
        }
    }
}
