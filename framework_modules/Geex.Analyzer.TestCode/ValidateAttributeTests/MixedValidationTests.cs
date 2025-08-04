
using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 混合使用有效和无效规则
    public class MixedValidationTest
    {
        // 有效规则
        [Validate(nameof(ValidateRule.Email))]
        [Validate("LengthMin?5")]
        public string ValidField { get; set; }

        // 无效规则 - 应该报告 GEEX003
        [Validate(nameof(ValidateRule.Null))]
        [Validate("AnotherInvalidRule?param")]
        public string InvalidField { get; set; }
    }
}
