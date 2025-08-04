using System;
using Geex.Validation;

namespace Geex.Analyzer.TestCode.ValidateAttributeTests
{
    // 复杂的验证规则使用
    public class ComplexValidateRuleTest
    {
        [Validate(nameof(ValidateRule.Regex), new object[] {@"^\d{4}-\d{2}-\d{2}$"}, "Invalid date format")]
        public string DateString { get; set; }

        [Validate(nameof(ValidateRule.FileExtension), new object[] {new[] {"jpg", "png", "gif"}})]
        public string ImageFile { get; set; }

        [Validate("DateRange?2020-01-01&2030-12-31")]
        public DateTime BirthDate { get; set; }

        [Validate("Regex?^\\d{3}-\\d{3}-\\d{4}$", "Invalid phone format")]
        public string PhoneNumber { get; set; }
    }
}
