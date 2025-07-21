using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = CSharpAnalyzerVerifier<BannedMediatRAnalyzer, DefaultVerifier>;

    public class BannedMediatRAnalyzerTests
    {
        [Fact]
        public async Task UsingMediatRNamespace_ShouldReportDiagnostic()
        {
            var test = """
                       using MediatR;
                       class C { }
                       """;
            var expected = AnalyzerVerifier.Diagnostic("GEEX001").WithSpan(2, 7, 2, 16).WithArguments("MediatR");
            await AnalyzerVerifier.VerifyAnalyzerAsync(test, new[] { expected });
        }

        [Fact]
        public async Task UsingBannedType_ShouldReportDiagnostic()
        {
            var test = """
                       class C
                       {
                           MediatR.IMediator mediator;
                       }
                       """;
            var expected = AnalyzerVerifier.Diagnostic("GEEX002").WithSpan(4, 5, 4, 24).WithArguments("MediatR.IMediator", "MediatX.IMediator");
            await AnalyzerVerifier.VerifyAnalyzerAsync(test, new[] { expected });
        }
    }
}
