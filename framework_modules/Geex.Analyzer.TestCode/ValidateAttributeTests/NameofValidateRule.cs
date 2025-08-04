using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 专门测试 nameof(ValidateRule.xxx) 形式的使用
    public class NameofValidateRuleTest
    {
        [Validate(nameof(ValidateRule.Email), "Please provide a valid email")]
        public string Email { get; set; }

        [Validate(nameof(ValidateRule.ChinesePhone), "Please provide a valid Chinese phone number")]
        public string Phone { get; set; }

        [Validate(nameof(ValidateRule.LengthMin), new object[] { 8 }, "Password must be at least 8 characters")]
        public string Password { get; set; }

        [Validate(nameof(ValidateRule.Range), new object[] { 0, 150 }, "Age must be between 0 and 150")]
        public int Age { get; set; }

        [Validate(nameof(ValidateRule.StrongPassword), "Password must be strong")]
        public string StrongPassword { get; set; }

        [Validate(nameof(ValidateRule.Url), "Please provide a valid URL")]
        public string Website { get; set; }
    }
}
