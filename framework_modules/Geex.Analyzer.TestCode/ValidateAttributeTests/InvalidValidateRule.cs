using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 无效的 ValidateRule 使用 nameof - 应该报告 GEEX003
    public class InvalidValidateRuleTest
    {
        [Validate(nameof(ValidateRule.Null), "message")]
        public string Field1 { get; set; }

        [Validate(nameof(ValidateRule.RuleKey))]
        public string Field2 { get; set; }
    }
}
