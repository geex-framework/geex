using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = CSharpAnalyzerVerifier<NullableAttributeAnalyzer, DefaultVerifier>;
    public class NullableAttributeAnalyzerTests
    {
        [Fact]
        public async Task PropertyWithDefaultValue_ShouldReportDiagnostic()
        {
            var test = """
                       public class C
                       {
                           public string Name { get; set; } = "";
                       }
                       """;
            var expected = AnalyzerVerifier.Diagnostic("GEEX003").WithSpan(4, 19, 4, 23).WithArguments("Name");
            await AnalyzerVerifier.VerifyAnalyzerAsync(test, new[] { expected });
        }

        [Fact]
        public async Task ParameterWithDefaultValue_ShouldReportDiagnostic()
        {
            var test = """
                       public class C
                       {
                           public void M(string name = "") { }
                       }
                       """;
            var expected = AnalyzerVerifier.Diagnostic("GEEX003").WithSpan(4, 22, 4, 26).WithArguments("name");
            await AnalyzerVerifier.VerifyAnalyzerAsync(test, new[] { expected });
        }
    }
}
