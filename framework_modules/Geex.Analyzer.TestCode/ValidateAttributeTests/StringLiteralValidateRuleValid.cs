using System;
using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 有效的字符串字面量验证规则
    public class StringLiteralValidateRuleValidTest
    {
        [Validate("Email", "Invalid email")]
        public string Email { get; set; }

        [Validate("Range?18&120", "Age must be between 18 and 120")]
        public int Age { get; set; }

        [Validate("LengthMin?6", "Password too short")]
        public string Password { get; set; }

        [Validate("ChinesePhone")]
        public string Phone { get; set; }
    }
}
