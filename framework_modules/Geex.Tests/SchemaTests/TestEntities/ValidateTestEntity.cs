using Geex.Validation;
using Geex.Gql.Types;
using Geex.Storage;
using HotChocolate.Types;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public class ValidateTestEntity : Entity<ValidateTestEntity>
    {
        public string ValidatedField { get; set; }
        public string EmailField { get; set; }
        public int AgeField { get; set; }
    }

    public class ValidateTestInput
    {
        [Validate(ValidateRuleKeys.Email, "Please provide a valid email address")]
        public string Email { get; set; }

        [Validate(ValidateRuleKeys.LengthMin, [6], "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Validate(ValidateRuleKeys.ChinesePhone, "Please provide a valid Chinese phone number")]
        public string Phone { get; set; }

        [Validate(ValidateRuleKeys.Range, [18, 120], "Age must be between 18 and 120")]
        public int Age { get; set; }
    }

    public class ValidateTestInputType : InputObjectType<ValidateTestInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<ValidateTestInput> descriptor)
        {
            // This shows both attribute-based validation (from class) and descriptor-based validation
            descriptor.Field(f => f.Email)
                .ApplyValidate(ValidateRule.Email(), "Please provide a valid email address")
                .ApplyValidate(ValidateRule.NotDisposableEmail(), "Disposable email addresses are not allowed");

            descriptor.Field(f => f.Password)
                .ApplyValidate(ValidateRule.LengthMin(6), "Password must be at least 6 characters")
                .ApplyValidate(ValidateRule.LengthMax(100), "Password cannot exceed 100 characters");

            descriptor.Field(f => f.Phone)
                .ApplyValidate(ValidateRule.ChinesePhone(), "Please provide a valid Chinese phone number");

            descriptor.Field(f => f.Age)
                .ApplyValidate(ValidateRule.Range(18, 120), "Age must be between 18 and 120");
        }
    }
}
