using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 测试不同参数类型的有效验证规则
    public class ParameterTypeTest
    {
        // 整数参数
        [Validate(nameof(ValidateRule.LengthMin), new object[] { 5 })]
        public string StringWithIntParam { get; set; }

        // 字符串参数
        [Validate(nameof(ValidateRule.Regex), new object[] { @"^[A-Z]+$" })]
        public string StringWithStringParam { get; set; }

        // 多个参数
        [Validate(nameof(ValidateRule.LengthRange), new object[] { 3, 20 })]
        public string StringWithMultipleParams { get; set; }

        // 布尔参数
        [Validate(nameof(ValidateRule.StrongPassword), new object[] { true, true, true, false })]
        public string PasswordWithBoolParams { get; set; }

        // 日期参数
        [Validate(nameof(ValidateRule.DateMin), new object[] { "2023-01-01" })]
        public DateTime? DateWithDateParam { get; set; }

        // 小数参数
        [Validate(nameof(ValidateRule.Min), new object[] { 0.5 })]
        public decimal DecimalWithDecimalParam { get; set; }

        // 数组参数
        [Validate(nameof(ValidateRule.FileExtension), new object[] { new string[] { ".jpg", ".png", ".gif" } })]
        public string FileWithArrayParam { get; set; }
    }
}
