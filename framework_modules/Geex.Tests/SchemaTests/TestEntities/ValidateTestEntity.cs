using Geex.Validation;
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
        [Validate(nameof(ValidateRule.Email), "Please provide a valid email address")]
        public string Email { get; set; }

        [Validate(nameof(ValidateRule.LengthMin), [6], "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Validate(nameof(ValidateRule.ChinesePhone), "Please provide a valid Chinese phone number")]
        public string Phone { get; set; }

        [Validate(nameof(ValidateRule.Range), [18, 120], "Age must be between 18 and 120")]
        public int Age { get; set; }
    }

    public class ValidateTestInputType : InputObjectType<ValidateTestInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<ValidateTestInput> descriptor)
        {
            // This shows both attribute-based validation (from class) and descriptor-based validation
            descriptor.TypedField(f => f.Email)
                .Validate(ValidateRule.Email(), "Please provide a valid email address")
                .Validate(ValidateRule.EmailNotDisposable(), "Disposable email addresses are not allowed");

            descriptor.TypedField(f => f.Password)
                .Validate(ValidateRule.LengthMin(6), "Password must be at least 6 characters")
                .Validate(ValidateRule.LengthMax(100), "Password cannot exceed 100 characters");

            descriptor.TypedField(f => f.Phone)
                .Validate(ValidateRule.ChinesePhone(), "Please provide a valid Chinese phone number");

            descriptor.TypedField(f => f.Age)
                .Validate(ValidateRule.Range(18, 120), "Age must be between 18 and 120");
        }
    }
}
