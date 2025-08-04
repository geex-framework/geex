using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 无效的字符串字面量验证规则 - 应该报告 GEEX003
    public class InvalidStringLiteralTest
    {
        [Validate("InvalidRule", "message")]
        public string Field1 { get; set; }

        [Validate("InvalidRule?param", "message")]
        public string Field2 { get; set; }

        [Validate("AnotherInvalidRule")]
        public string Field3 { get; set; }

        [Validate("NotExistingRule?param1&param2")]
        public string Field4 { get; set; }

        [Validate("WrongRuleName?value")]
        public string Field5 { get; set; }
    }
}
