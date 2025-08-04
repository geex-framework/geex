using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 测试边界情况的验证规则
    public class EdgeCaseValidationRulesTest
    {
        // 空字符串规则
        [Validate("", "Empty rule name")]
        public string EmptyRuleField { get; set; }

        // 只有参数没有规则名
        [Validate("", new object[] { "param" }, "Empty rule with parameters")]
        public string EmptyRuleWithParamsField { get; set; }

        // 无效的参数组合
        [Validate(nameof(ValidateRule.LengthMin), new object[] { "invalid" })]
        public string InvalidParamTypeField { get; set; }

        // 参数数量不匹配
        [Validate(nameof(ValidateRule.LengthRange), new object[] { 5 })] // 应该有两个参数
        public string MissingParamField { get; set; }

        [Validate(nameof(ValidateRule.LengthMin), new object[] { 5, 10, 15 })] // 应该只有一个参数
        public string ExtraParamField { get; set; }
    }
}
