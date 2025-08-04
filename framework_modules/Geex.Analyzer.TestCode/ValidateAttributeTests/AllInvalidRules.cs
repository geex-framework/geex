using System;
using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 测试所有无效规则应该报告多个诊断
    public class AllInvalidRulesTest
    {
        // nameof 方式的无效规则
        [Validate(nameof(ValidateRule.Null), "Invalid rule message")]
        public string Field1 { get; set; }

        [Validate(nameof(ValidateRule.RuleKey))]
        public string Field2 { get; set; }

        // 字符串字面量方式的无效规则
        [Validate("InvalidRule", "Invalid rule message")]
        public string Field3 { get; set; }

        [Validate("InvalidRule", new object[] { "param" })]
        public string Field4 { get; set; }

        [Validate("AnotherInvalidRule", "Another invalid rule")]
        public string Field5 { get; set; }

        [Validate("NotExistingRule")]
        public string Field6 { get; set; }

        [Validate("WrongRuleName", new object[] { 1, 2 })]
        public string Field7 { get; set; }

        // 空字符串和只有参数的无效规则
        [Validate("", "Empty rule")]
        public string Field8 { get; set; }

        [Validate("", new object[] { "param" })]
        public string Field9 { get; set; }
    }
}
