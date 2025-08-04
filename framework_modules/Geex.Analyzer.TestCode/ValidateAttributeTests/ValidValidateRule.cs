using System;
using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 有效的 ValidateRule 使用 nameof
    public class ValidValidateRuleTest
    {
        [Validate(nameof(ValidateRule.ChinesePhone), "可选的message")]
        public string Phone { get; set; }

        [Validate(nameof(ValidateRule.Range), new object[] {18, 120}, "可选的message")]
        public int Age { get; set; }

        [Validate(nameof(ValidateRule.Email))]
        public string Email { get; set; }

        [Validate(nameof(ValidateRule.LengthMin), new object[] {6}, "Password must be at least 6 characters")]
        [Validate(nameof(ValidateRule.StrongPassword))]
        public string Password { get; set; }
    }
}
