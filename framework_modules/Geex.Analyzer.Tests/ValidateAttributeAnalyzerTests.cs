using System.Threading.Tasks;
using Geex.Analyzer.Analyzer;
using Geex.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = CSharpAnalyzerVerifier<ValidateAttributeAnalyzer, GeexOnlyVerifier>;

    public class ValidateAttributeAnalyzerTests
    {
        [Fact]
        public async Task ValidValidateRule_ShouldNotReportDiagnostic()
        {
            var test = """
                       using Geex.Validation;
                       
                       namespace Geex.Validation
                       {
                           public static class ValidateRule
                           {
                               public static string ChinesePhone => "ChinesePhone";
                               public static string Range => "Range";
                               public static string Email => "Email";
                           }
                           public class ValidateAttribute : System.Attribute
                           {
                               public ValidateAttribute(string ruleName, params object[] args) { }
                           }
                       }

                       public class TestClass
                       {
                           [Validate(nameof(ValidateRule.ChinesePhone), "可选的message")]
                           public string Phone { get; set; }

                           [Validate(nameof(ValidateRule.Range), new object[] { 18, 120 }, "可选的message")]
                           public int Age { get; set; }

                           [Validate(nameof(ValidateRule.Email))]
                           public string Email { get; set; }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task InvalidValidateRule_ShouldReportDiagnostic()
        {
            var test = """
                       using Geex.Validation;
                       namespace Geex.Validation
                       {
                           public static class ValidateRule
                           {
                               public static string InvalidRule => "This rule does not exist";
                           }
                           public class ValidateAttribute : System.Attribute
                           {
                               public ValidateAttribute(string ruleName, params object[] args) { }
                           }
                       }

                       public class TestClass
                       {
                           [Validate(nameof(ValidateRule.InvalidRule), "message")]
                           public string Field { get; set; }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(13, 47, 13, 58)
                .WithArguments("InvalidRule");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task StringLiteralValidateRule_ValidRule_ShouldNotReportDiagnostic()
        {
            var test = """
                       using Geex.Validation;
                       
                       namespace Geex.Validation
                       {
                           public class ValidateAttribute : System.Attribute
                           {
                               public ValidateAttribute(string ruleName, params object[] args) { }
                           }
                       }

                       public class TestClass
                       {
                           [Validate("Email", "Invalid email")]
                           public string Email { get; set; }

                           [Validate("Range?18&120", "Age must be between 18 and 120")]
                           public int Age { get; set; }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task StringLiteralValidateRule_InvalidRule_ShouldReportDiagnostic()
        {
            var test = """
                       using Geex.Validation;
                       
                       namespace Geex.Validation
                       {
                           public class ValidateAttribute : System.Attribute
                           {
                               public ValidateAttribute(string ruleName, params object[] args) { }
                           }
                       }

                       public class TestClass
                       {
                           [Validate("InvalidRule", "message")]
                           public string Field { get; set; }

                           [Validate("InvalidRule?param", "message")]
                           public string Field2 { get; set; }
                       }
                       """;

            var expected1 = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(13, 23, 13, 36)
                .WithArguments("InvalidRule");

            var expected2 = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(16, 23, 16, 41)
                .WithArguments("InvalidRule");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [Fact]
        public async Task ComplexValidateRule_ShouldNotReportDiagnostic()
        {
            var test = """
                       using Geex.Validation;
                       
                       namespace Geex.Validation
                       {
                           public static class ValidateRule
                           {
                               public static string LengthMin => "LengthMin";
                               public static string StrongPassword => "StrongPassword";
                           }
                           public class ValidateAttribute : System.Attribute
                           {
                               public ValidateAttribute(string ruleName, params object[] args) { }
                           }
                       }

                       public class TestClass
                       {
                           [Validate(nameof(ValidateRule.LengthMin), new object[] { 6 }, "Password must be at least 6 characters")]
                           [Validate(nameof(ValidateRule.StrongPassword))]
                           public string Password { get; set; }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }
    }
}
