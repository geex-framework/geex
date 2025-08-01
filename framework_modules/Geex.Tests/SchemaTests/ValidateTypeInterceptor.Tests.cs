using Geex.Validation;
using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;
using Geex.Tests.FeatureTests;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class ValidateTestQuery : QueryExtension<ValidateTestQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ValidateTestQuery> descriptor)
        {
            descriptor.Field(x => x.ValidateTestQueryField(default, default))
                .Argument("email", argumentDescriptor => argumentDescriptor.ApplyValidate(
                    ValidateRule.Email(), "Invalid email format"))
                .Argument("name", argumentDescriptor => argumentDescriptor.ApplyValidate(
                    ValidateRule.LengthMin(3), "Name must be at least 3 characters"));
            base.Configure(descriptor);
        }

        public ValidateTestEntity ValidateTestQueryField(
            [Validate(ValidateRuleKeys.Email, "Invalid email format")] string email,
            [Validate(ValidateRuleKeys.LengthMin, [3], "Name must be at least 3 characters")] string name) => throw new NotImplementedException();

        public ValidateTestEntity ValidateTestQueryFieldWithInput(ValidateTestInput input) => throw new NotImplementedException();
    }

    public class ValidateTestMutation : MutationExtension<ValidateTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ValidateTestMutation> descriptor)
        {
            descriptor.Field(x => x.ValidateTestMutationField(default, default))
                .Argument("name", argumentDescriptor => argumentDescriptor.ApplyValidate(
                ValidateRule.LengthMin(3), "Name must be at least 3 characters"))
                .Argument("count", argumentDescriptor => argumentDescriptor.ApplyValidate(
                ValidateRule.Range(1, 100), "Count must be between 1 and 100"))
                ;
            base.Configure(descriptor);
        }

        public bool ValidateTestMutationField(
            [Validate(ValidateRuleKeys.LengthMin, [3], "Name must be at least 3 characters")] string name,
            [Validate(ValidateRuleKeys.Range, [1, 100], "Count must be between 1 and 100")] int count) => throw new NotImplementedException();

        public bool CreateUser(ValidateTestInput input) => throw new NotImplementedException();
    }

    [Collection(nameof(TestsCollection))]
    public class ValidateTypeInterceptorTests : TestsBase
    {
        public ValidateTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ValidationDirectivesShouldBeAppliedToFieldsWithValidationDirectives()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the query type
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            queryType.ShouldNotBeNull();

            // Find the test query field with validation directives
            var validateQueryField = queryType.Fields.FirstOrDefault(x => x.Name == nameof(ValidateTestQuery.ValidateTestQueryField).ToCamelCase());
            validateQueryField.ShouldNotBeNull();

            // Check if validation directives are present on arguments
            var emailArgument = validateQueryField.Arguments.FirstOrDefault(x => x.Name == "email");
            emailArgument.ShouldNotBeNull();
            var hasEmailValidation = emailArgument.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasEmailValidation.ShouldBeTrue("Email argument should have validation directive");

            var nameArgument = validateQueryField.Arguments.FirstOrDefault(x => x.Name == "name");
            nameArgument.ShouldNotBeNull();
            var hasNameValidation = nameArgument.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasNameValidation.ShouldBeTrue("Name argument should have validation directive");
        }

        [Fact]
        public async Task ValidationDirectivesShouldBeAppliedToMutationFields()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the mutation type
            var mutationType = schema.GetType<ObjectType>(nameof(Mutation));
            mutationType.ShouldNotBeNull();

            // Find the test mutation field with validation directives
            var validateMutationField = mutationType.Fields.FirstOrDefault(x => x.Name == nameof(ValidateTestMutation.ValidateTestMutationField).ToCamelCase());
            validateMutationField.ShouldNotBeNull();

            // Check if validation directives are present on arguments
            var nameArgument = validateMutationField.Arguments.FirstOrDefault(x => x.Name == "name");
            nameArgument.ShouldNotBeNull();
            var hasNameValidation = nameArgument.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasNameValidation.ShouldBeTrue("Name argument should have validation directive");

            var countArgument = validateMutationField.Arguments.FirstOrDefault(x => x.Name == "count");
            countArgument.ShouldNotBeNull();
            var hasCountValidation = countArgument.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasCountValidation.ShouldBeTrue("Count argument should have validation directive");
        }

        [Fact]
        public async Task ValidationDirectivesShouldBeDetectedOnArguments()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the query type
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            var validateQueryField = queryType.Fields.FirstOrDefault(x => x.Name == nameof(ValidateTestQuery.ValidateTestQueryField).ToCamelCase());
            validateQueryField.ShouldNotBeNull();

            // Check if the email argument has validation directives
            var emailArgument = validateQueryField.Arguments.FirstOrDefault(x => x.Name == "email");
            emailArgument.ShouldNotBeNull();

            // The argument should have validation directives applied
            var hasValidateDirective = emailArgument.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasValidateDirective.ShouldBeTrue("Email argument should have validate directive");
        }

        [Fact]
        public async Task InputObjectValidationShouldBeDetected()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the input object type
            var inputType = schema.GetType<InputObjectType>(nameof(ValidateTestInput));
            inputType.ShouldNotBeNull();

            // Check if validation directives are present on input fields
            var emailField = inputType.Fields.FirstOrDefault(f => f.Name == "email");
            emailField.ShouldNotBeNull();
            var hasEmailValidation = emailField.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasEmailValidation.ShouldBeTrue("Email field should have validation directive");

            var passwordField = inputType.Fields.FirstOrDefault(f => f.Name == "password");
            passwordField.ShouldNotBeNull();
            var hasPasswordValidation = passwordField.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasPasswordValidation.ShouldBeTrue("Password field should have validation directive");

            var phoneField = inputType.Fields.FirstOrDefault(f => f.Name == "phone");
            phoneField.ShouldNotBeNull();
            var hasPhoneValidation = phoneField.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasPhoneValidation.ShouldBeTrue("Phone field should have validation directive");

            var ageField = inputType.Fields.FirstOrDefault(f => f.Name == "age");
            ageField.ShouldNotBeNull();
            var hasAgeValidation = ageField.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasAgeValidation.ShouldBeTrue("Age field should have validation directive");
        }

        [Fact]
        public async Task ValidateAttributeShouldBeConvertedToDirectives()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the input object type that uses ValidateAttribute
            var inputType = schema.GetType<InputObjectType>(nameof(ValidateTestInput));
            inputType.ShouldNotBeNull();

            // Check if attributes were converted to directives
            var emailField = inputType.Fields.FirstOrDefault(f => f.Name == "email");
            emailField.ShouldNotBeNull();

            // Should have both attribute-based and descriptor-based validation directives
            var emailDirectives = emailField.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            emailDirectives.Count.ShouldBeGreaterThanOrEqualTo(2, "Email field should have multiple validation directives (from attribute and descriptor)");

            var passwordField = inputType.Fields.FirstOrDefault(f => f.Name == "password");
            passwordField.ShouldNotBeNull();
            var passwordDirectives = passwordField.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            passwordDirectives.Count.ShouldBeGreaterThanOrEqualTo(2, "Password field should have multiple validation directives (MinLength from attribute, MinLength and MaxLength from descriptor)");

            var phoneField = inputType.Fields.FirstOrDefault(f => f.Name == "phone");
            phoneField.ShouldNotBeNull();
            var phoneDirectives = phoneField.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            phoneDirectives.Count.ShouldBeGreaterThanOrEqualTo(1, "Phone field should have validation directive from attribute and descriptor");

            var ageField = inputType.Fields.FirstOrDefault(f => f.Name == "age");
            ageField.ShouldNotBeNull();
            var ageDirectives = ageField.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            ageDirectives.Count.ShouldBeGreaterThanOrEqualTo(1, "Age field should have validation directive from attribute and descriptor");
        }

        [Fact]
        public async Task ValidateAttributeOnParametersShouldWork()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the query type
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            var validateQueryField = queryType.Fields.FirstOrDefault(x => x.Name == nameof(ValidateTestQuery.ValidateTestQueryField).ToCamelCase());
            validateQueryField.ShouldNotBeNull();

            // Check if parameters with ValidateAttribute have validation directives
            // Note: These should have both attribute-based and descriptor-based validation
            var emailArgument = validateQueryField.Arguments.FirstOrDefault(x => x.Name == "email");
            emailArgument.ShouldNotBeNull();
            var emailDirectives = emailArgument.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            emailDirectives.Count.ShouldBeGreaterThanOrEqualTo(1, "Email parameter should have validation directives from attribute and/or descriptor");

            var nameArgument = validateQueryField.Arguments.FirstOrDefault(x => x.Name == "name");
            nameArgument.ShouldNotBeNull();
            var nameDirectives = nameArgument.Directives.Where(d => d.Type.Name == ValidateDirective.DirectiveName).ToList();
            nameDirectives.Count.ShouldBeGreaterThanOrEqualTo(1, "Name parameter should have validation directives from attribute and/or descriptor");
        }

        [Fact]
        public async Task FieldsWithoutValidationDirectivesShouldNotHaveValidationDirectives()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Get the entity type
            var entityType = schema.GetType<ObjectType>(nameof(ValidateTestEntity));
            entityType.ShouldNotBeNull();

            // Check common fields that shouldn't have validation directives
            var commonFields = new[] { "id", "createdOn", "updatedOn" };

            foreach (var fieldName in commonFields)
            {
                var field = entityType.Fields.FirstOrDefault(f => f.Name == fieldName);
                if (field != null)
                {
                    // These fields should not have validation directives
                    var hasValidationDirective = field.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
                    hasValidationDirective.ShouldBeFalse($"Field '{fieldName}' should not have validation directive");
                }
            }
        }

        [Fact]
        public async Task ValidateDirectiveTypeShouldBeRegistered()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Check if ValidateDirective is registered in the schema
            var validateDirectiveType = schema.DirectiveTypes.FirstOrDefault(d => d.Name == ValidateDirective.DirectiveName);
            validateDirectiveType.ShouldNotBeNull("ValidateDirective should be registered in the schema");

            // Check directive locations - now includes FieldDefinition
            var expectedLocations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition | DirectiveLocation.FieldDefinition;
            validateDirectiveType.Locations.ShouldBe(expectedLocations);

            // Check if directive is repeatable
            validateDirectiveType.IsRepeatable.ShouldBeTrue("ValidateDirective should be repeatable");
        }

        [Fact]
        public async Task ValidationRulesShouldBeProperlyConfigured()
        {
            // Test email validation rule
            var emailRule = ValidateRule.Email();
            emailRule.ShouldNotBeNull();
            emailRule.RuleKey.ShouldNotBeNullOrEmpty();

            var validEmail = "test@example.com";
            var invalidEmail = "invalid-email";

            var validResult = emailRule.Validate(validEmail);
            validResult.ErrorMessage.ShouldBeNullOrEmpty();

            var invalidResult = emailRule.Validate(invalidEmail);
            invalidResult.ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test MinLength validation rule
            var minLengthRule = ValidateRule.LengthMin(6);
            minLengthRule.ShouldNotBeNull();

            var validPassword = "password123";
            var invalidPassword = "123";

            var validPasswordResult = minLengthRule.Validate(validPassword);
            validPasswordResult.ErrorMessage.ShouldBeNullOrEmpty();

            var invalidPasswordResult = minLengthRule.Validate(invalidPassword);
            invalidPasswordResult.ErrorMessage.ShouldNotBeNullOrEmpty();
        }
    }
}
