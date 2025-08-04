using System;

using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 测试所有支持的有效验证规则
    public class AllValidRulesTest
    {
        // 字符串验证规则
        [Validate(nameof(ValidateRule.Email))]
        public string Email { get; set; }

        [Validate(nameof(ValidateRule.EmailNotDisposable))]
        public string PermanentEmail { get; set; }

        [Validate(nameof(ValidateRule.ChinesePhone))]
        public string Phone { get; set; }

        [Validate(nameof(ValidateRule.Url))]
        public string Website { get; set; }

        [Validate(nameof(ValidateRule.CreditCard))]
        public string CreditCard { get; set; }

        [Validate(nameof(ValidateRule.Json))]
        public string JsonData { get; set; }

        [Validate(nameof(ValidateRule.IPv4))]
        public string IPv4Address { get; set; }

        [Validate(nameof(ValidateRule.IPv6))]
        public string IPv6Address { get; set; }

        [Validate(nameof(ValidateRule.IP))]
        public string IPAddress { get; set; }

        [Validate(nameof(ValidateRule.MacAddress))]
        public string MacAddress { get; set; }

        [Validate(nameof(ValidateRule.Guid))]
        public string GuidString { get; set; }

        [Validate(nameof(ValidateRule.Alpha))]
        public string AlphaField { get; set; }

        [Validate(nameof(ValidateRule.Numeric))]
        public string NumericField { get; set; }

        [Validate(nameof(ValidateRule.AlphaNumeric))]
        public string AlphaNumericField { get; set; }

        [Validate(nameof(ValidateRule.NoWhitespace))]
        public string NoSpaceField { get; set; }

        [Validate(nameof(ValidateRule.StrongPassword))]
        public string Password { get; set; }

        [Validate(nameof(ValidateRule.ChineseIdCard))]
        public string IdCard { get; set; }

        // 带参数的规则
        [Validate(nameof(ValidateRule.LengthMin), new object[] { 3 })]
        public string MinLengthField { get; set; }

        [Validate(nameof(ValidateRule.LengthMax), new object[] { 100 })]
        public string MaxLengthField { get; set; }

        [Validate(nameof(ValidateRule.LengthRange), new object[] { 5, 50 })]
        public string RangeLengthField { get; set; }

        [Validate(nameof(ValidateRule.Regex), new object[] { @"^\d{3}-\d{3}-\d{4}$" })]
        public string PhonePattern { get; set; }

        // 数值验证规则
        [Validate(nameof(ValidateRule.Min), new object[] { 0 })]
        public int MinValue { get; set; }

        [Validate(nameof(ValidateRule.Max), new object[] { 1000 })]
        public int MaxValue { get; set; }

        [Validate(nameof(ValidateRule.Range), new object[] { 18, 65 })]
        public int Age { get; set; }

        [Validate(nameof(ValidateRule.Price))]
        public decimal Price { get; set; }

        // 日期验证规则
        [Validate(nameof(ValidateRule.DateFuture))]
        public DateTime? FutureDate { get; set; }

        [Validate(nameof(ValidateRule.DatePast))]
        public DateTime? PastDate { get; set; }

        [Validate(nameof(ValidateRule.BirthDateMinAge), new object[] { 18 })]
        public DateTime? BirthDate { get; set; }
    }
}
