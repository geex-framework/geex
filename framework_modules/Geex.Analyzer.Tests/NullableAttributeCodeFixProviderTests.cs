using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;
namespace Geex.Analyzer.Tests
{
    using CodeFixVerifier = CSharpCodeFixVerifier<NullableAttributeAnalyzer, NullableAttributeCodeFixProvider, DefaultVerifier>;

    public class NullableAttributeCodeFixProviderTests
    {
        [Fact]
        public async Task PropertyWithDefaultValue_ShouldAddNullableAttribute()
        {
            var test = """
                       public class C
                       {
                           public string Name { get; set; } = "";
                       }
                       """;
            var fixedCode = """
                            public class C
                            {
                                [Nullable]
                                public string Name { get; set; } = "";
                            }
                            """;
            var expected = CodeFixVerifier.Diagnostic("GEEX003").WithSpan(3, 23, 3, 27).WithArguments("Name");
            await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task ParameterWithDefaultValue_ShouldAddNullableAttribute()
        {
            var test = """
                       public class C
                       {
                           public void M(string name = "") { }
                       }
                       """;
            var fixedCode = """
                            public class C
                            {
                                public void M([Nullable] string name = "") { }
                            }
                            """;
            var expected = CodeFixVerifier.Diagnostic("GEEX003").WithSpan(3, 26, 3, 30).WithArguments("name");
            await CodeFixVerifier.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
