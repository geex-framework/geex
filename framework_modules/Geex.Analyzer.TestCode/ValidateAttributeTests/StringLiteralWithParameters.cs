using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 测试带参数的字符串字面量形式的验证规则
    public class StringLiteralWithParametersTest
    {
        [Validate("LengthMin", new object[] { 6 }, "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Validate("Range", new object[] { 18, 120 }, "Age must be between 18 and 120")]
        public int Age { get; set; }

        [Validate("LengthRange", new object[] { 3, 50 }, "Name must be between 3 and 50 characters")]
        public string Name { get; set; }

        [Validate("Regex", new object[] { @"^[A-Za-z0-9]+$" }, "Username can only contain letters and numbers")]
        public string Username { get; set; }
    }
}
