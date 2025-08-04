using System.Threading.Tasks;
using Geex.Analyzer.Analyzers;
using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<ValidateAttributeAnalyzer>;

    /// <summary>
    /// ValidateAttributeAnalyzer 的测试类
    /// 使用 Geex.Analyzer.TestCode 项目中的真实代码进行测试
    /// </summary>
    public class ValidateAttributeAnalyzerTests
    {
        [Fact]
        public async Task ValidValidateRule_ShouldNotReportDiagnostic()
        {
            // 测试有效的 ValidateRule 使用不应该报告任何诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/ValidValidateRule.cs");
        }

        [Fact]
        public async Task InvalidValidateRule_ShouldReportDiagnostic()
        {
            // 测试无效的 ValidateRule 应该报告 GEEX003 诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("Null"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("RuleKey")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("ValidateAttributeTests/InvalidValidateRule.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task StringLiteralValidateRule_ValidRule_ShouldNotReportDiagnostic()
        {
            // 测试有效的字符串字面量验证规则
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/StringLiteralValidateRuleValid.cs");
        }

        [Fact]
        public async Task StringLiteralValidateRule_InvalidRule_ShouldReportDiagnostic()
        {
            // 测试无效的字符串字面量验证规则应该报告 GEEX003 诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("InvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("InvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("AnotherInvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("NotExistingRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("WrongRuleName")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("ValidateAttributeTests/StringLiteralValidateRuleInvalid.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task ComplexValidateRule_ShouldNotReportDiagnostic()
        {
            // 测试复杂的验证规则使用
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/ComplexValidateRule.cs");
        }

        [Fact]
        public async Task MixedValidationTests_ShouldReportOnlyInvalidRules()
        {
            // 测试混合使用有效和无效规则，只有无效规则应该报告诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("Null"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("AnotherInvalidRule")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("ValidateAttributeTests/MixedValidationTests.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task EdgeCaseValidationRules_ShouldReportInvalidRules()
        {
            // 测试边界情况的验证规则
            var expectedDiagnostics = new[]
            {
                // 空字符串规则
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments(""),

                // 只有参数没有规则名
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("ValidateAttributeTests/EdgeCaseValidationRules.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task ParameterTypeTests_ShouldNotReportDiagnostic()
        {
            // 测试不同参数类型的有效验证规则
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/ParameterTypeTests.cs");
        }

        [Fact]
        public async Task AllValidRules_ShouldNotReportDiagnostics()
        {
            // 测试所有支持的有效验证规则
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/AllValidRules.cs");
        }

        [Fact]
        public async Task AllInvalidRules_ShouldReportMultipleDiagnostics()
        {
            // 测试所有无效规则应该报告多个诊断
            var expectedDiagnostics = new[]
            {
                // nameof 方式的无效规则
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("Null"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("RuleKey"),

                // 字符串字面量方式的无效规则
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("InvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("InvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("AnotherInvalidRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("NotExistingRule"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("WrongRuleName"),

                // 空字符串和只有参数的无效规则
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments(""),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("ValidateAttributeTests/AllInvalidRules.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task NameofValidateRule_ShouldValidateCorrectly()
        {
            // 专门测试 nameof(ValidateRule.xxx) 形式的使用
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/NameofValidateRule.cs");
        }

        [Fact]
        public async Task StringLiteralWithParameters_ShouldValidateCorrectly()
        {
            // 专门测试带参数的字符串字面量形式
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("ValidateAttributeTests/StringLiteralWithParameters.cs");
        }
    }
}
