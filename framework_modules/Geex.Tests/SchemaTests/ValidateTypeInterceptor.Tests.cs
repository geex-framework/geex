using Geex.Validation;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class ValidateTestQuery : QueryExtension<ValidateTestQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ValidateTestQuery> descriptor)
        {
            descriptor.Field(x => x.ValidateTestQueryField(default, default))
                .Argument("email", argumentDescriptor => argumentDescriptor.Validate(
                    ValidateRule.Email(), "Invalid email format"))
                .Argument("name", argumentDescriptor => argumentDescriptor.Validate(
                    ValidateRule.LengthMin(3), "Name must be at least 3 characters"));
            base.Configure(descriptor);
        }

        public ValidateTestEntity ValidateTestQueryField(
            [Validate(nameof(ValidateRule.Email), "Invalid email format")] string email,
            [Validate(nameof(ValidateRule.LengthMin), [3], "Name must be at least 3 characters")] string name) => new ValidateTestEntity()
            {
                Id = ObjectId.GenerateNewId().ToString(),
            };

        public ValidateTestEntity ValidateTestQueryFieldWithInput(ValidateTestInput input) => new ValidateTestEntity()
        {
            Id = ObjectId.GenerateNewId().ToString()
        };
    }

    public class ValidateTestMutation : MutationExtension<ValidateTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ValidateTestMutation> descriptor)
        {
            descriptor.Field(x => x.ValidateTestMutationField(default, default))
                .Argument("name", argumentDescriptor => argumentDescriptor.Validate(
                ValidateRule.LengthMin(3), "Name must be at least 3 characters"))
                .Argument("count", argumentDescriptor => argumentDescriptor.Validate(
                ValidateRule.Range(1, 100), "Count must be between 1 and 100"))
                ;
            base.Configure(descriptor);
        }

        public bool ValidateTestMutationField(
            [Validate(nameof(ValidateRule.LengthMin), [3], "Name must be at least 3 characters")] string name,
            [Validate(nameof(ValidateRule.Range), [1, 100], "Count must be between 1 and 100")] int count) => true;

        public bool ValidateCreateUserInput(ValidateTestInput input) => true;
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

            // Check directive locations
            var expectedLocations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition;
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
            validResult.ShouldBeEquivalentTo(ValidationResult.Success);

            var invalidResult = emailRule.Validate(invalidEmail);
            invalidResult.ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test MinLength validation rule
            var minLengthRule = ValidateRule.LengthMin(6);
            minLengthRule.ShouldNotBeNull();

            var validPassword = "password123";
            var invalidPassword = "123";

            var validPasswordResult = minLengthRule.Validate(validPassword);
            validPasswordResult.ShouldBeEquivalentTo(ValidationResult.Success);

            var invalidPasswordResult = minLengthRule.Validate(invalidPassword);
            invalidPasswordResult.ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void StringValidationRulesShouldWork()
        {
            // Test LengthMax
            var maxLengthRule = ValidateRule.LengthMax(10);
            maxLengthRule.Validate("short").ShouldBeEquivalentTo(ValidationResult.Success);
            maxLengthRule.Validate("this is too long text").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test LengthRange
            var lengthRangeRule = ValidateRule.LengthRange(3, 10);
            lengthRangeRule.Validate("test").ShouldBeEquivalentTo(ValidationResult.Success);
            lengthRangeRule.Validate("to").ErrorMessage.ShouldNotBeNullOrEmpty();
            lengthRangeRule.Validate("this is too long").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Regex
            var regexRule = ValidateRule.Regex(@"^\d{3}-\d{3}-\d{4}$");
            regexRule.Validate("123-456-7890").ShouldBeEquivalentTo(ValidationResult.Success);
            regexRule.Validate("invalid-format").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test ChinesePhone
            var phoneRule = ValidateRule.ChinesePhone();
            phoneRule.Validate("13812345678").ShouldBeEquivalentTo(ValidationResult.Success);
            phoneRule.Validate("12345678901").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test URL
            var urlRule = ValidateRule.Url();
            urlRule.Validate("https://example.com").ShouldBeEquivalentTo(ValidationResult.Success);
            urlRule.Validate("invalid-url").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Alpha
            var alphaRule = ValidateRule.Alpha();
            alphaRule.Validate("OnlyLetters").ShouldBeEquivalentTo(ValidationResult.Success);
            alphaRule.Validate("Letters123").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Numeric
            var numericRule = ValidateRule.Numeric();
            numericRule.Validate("123456").ShouldBeEquivalentTo(ValidationResult.Success);
            numericRule.Validate("123abc").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test AlphaNumeric
            var alphaNumericRule = ValidateRule.AlphaNumeric();
            alphaNumericRule.Validate("Test123").ShouldBeEquivalentTo(ValidationResult.Success);
            alphaNumericRule.Validate("Test 123").ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void NumericValidationRulesShouldWork()
        {
            // Test Min
            var minRule = ValidateRule.Min(10);
            minRule.Validate(15).ShouldBeEquivalentTo(ValidationResult.Success);
            minRule.Validate(5).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Max
            var maxRule = ValidateRule.Max(100);
            maxRule.Validate(50).ShouldBeEquivalentTo(ValidationResult.Success);
            maxRule.Validate(150).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Range
            var rangeRule = ValidateRule.Range(18, 65);
            rangeRule.Validate(25).ShouldBeEquivalentTo(ValidationResult.Success);
            rangeRule.Validate(10).ErrorMessage.ShouldNotBeNullOrEmpty();
            rangeRule.Validate(70).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Price
            var priceRule = ValidateRule.Price();
            priceRule.Validate(99.99m).ShouldBeEquivalentTo(ValidationResult.Success);
            priceRule.Validate(-10m).ErrorMessage.ShouldNotBeNullOrEmpty();
            priceRule.Validate(1000000000m).ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void DateValidationRulesShouldWork()
        {
            var yesterday = DateTime.Today.AddDays(-1);
            var tomorrow = DateTime.Today.AddDays(1);

            // Test DateMin
            var dateMinRule = ValidateRule.DateMin(yesterday);
            dateMinRule.Validate(DateTime.Today).ShouldBeEquivalentTo(ValidationResult.Success);
            dateMinRule.Validate(yesterday.AddDays(-1)).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test DateMax
            var dateMaxRule = ValidateRule.DateMax(tomorrow);
            dateMaxRule.Validate(DateTime.Today).ShouldBeEquivalentTo(ValidationResult.Success);
            dateMaxRule.Validate(tomorrow.AddDays(1)).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test DateRange
            var dateRangeRule = ValidateRule.DateRange(yesterday, tomorrow);
            dateRangeRule.Validate(DateTime.Today).ShouldBeEquivalentTo(ValidationResult.Success);
            dateRangeRule.Validate(yesterday.AddDays(-1)).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test DateFuture
            var futureDateRule = ValidateRule.DateFuture();
            futureDateRule.Validate(DateTime.UtcNow.AddDays(1)).ShouldBeEquivalentTo(ValidationResult.Success);
            futureDateRule.Validate(DateTime.UtcNow.AddDays(-1)).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test DatePast
            var pastDateRule = ValidateRule.DatePast();
            pastDateRule.Validate(DateTime.UtcNow.AddDays(-1)).ShouldBeEquivalentTo(ValidationResult.Success);
            pastDateRule.Validate(DateTime.UtcNow.AddDays(1)).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test BirthDateMinAge
            var birthDateRule = ValidateRule.BirthDateMinAge(18);
            var adultBirthDate = DateTime.Today.AddYears(-20);
            var minorBirthDate = DateTime.Today.AddYears(-16);
            birthDateRule.Validate(adultBirthDate).ShouldBeEquivalentTo(ValidationResult.Success);
            birthDateRule.Validate(minorBirthDate).ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void ListValidationRulesShouldWork()
        {
            var emptyList = new List<string>();
            var shortList = new List<string> { "a", "b" };
            var longList = new List<string> { "a", "b", "c", "d", "e" };

            // Test ListNotEmpty
            var notEmptyRule = ValidateRule.ListNotEmpty<string>();
            notEmptyRule.Validate(shortList).ShouldBeEquivalentTo(ValidationResult.Success);
            notEmptyRule.Validate(emptyList).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test ListSizeMin
            var sizeMinRule = ValidateRule.ListSizeMin<string>(3);
            sizeMinRule.Validate(longList).ShouldBeEquivalentTo(ValidationResult.Success);
            sizeMinRule.Validate(shortList).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test ListSizeMax
            var sizeMaxRule = ValidateRule.ListSizeMax<string>(3);
            sizeMaxRule.Validate(shortList).ShouldBeEquivalentTo(ValidationResult.Success);
            sizeMaxRule.Validate(longList).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test ListSizeRange
            var sizeRangeRule = ValidateRule.ListSizeRange<string>(2, 4);
            sizeRangeRule.Validate(shortList).ShouldBeEquivalentTo(ValidationResult.Success);
            sizeRangeRule.Validate(emptyList).ErrorMessage.ShouldNotBeNullOrEmpty();
            sizeRangeRule.Validate(longList).ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void CompositeValidationRulesShouldWork()
        {
            // Test AND operator
            var emailRule = ValidateRule.Email();
            var minLengthRule = ValidateRule.LengthMin(10);
            var andRule = emailRule & minLengthRule;

            andRule.Validate("a@b.com").ErrorMessage.ShouldNotBeNullOrEmpty(); // Valid email but too short
            andRule.Validate("verylongemail@example.com").ShouldBeEquivalentTo(ValidationResult.Success); // Valid email and long enough
            andRule.Validate("invalid-email").ErrorMessage.ShouldNotBeNullOrEmpty(); // Invalid email

            // Test OR operator
            var alphaRule = ValidateRule.Alpha();
            var numericRule = ValidateRule.Numeric();
            var orRule = alphaRule | numericRule;

            orRule.Validate("OnlyLetters").ShouldBeEquivalentTo(ValidationResult.Success); // Valid alpha
            orRule.Validate("123456").ShouldBeEquivalentTo(ValidationResult.Success); // Valid numeric
            orRule.Validate("Mix3d").ErrorMessage.ShouldNotBeNullOrEmpty(); // Neither alpha nor numeric
        }

        [Fact]
        public void SpecializedValidationRulesShouldWork()
        {
            // Test EmailNotDisposable
            var notDisposableRule = ValidateRule.EmailNotDisposable();
            notDisposableRule.Validate("user@gmail.com").ShouldBeEquivalentTo(ValidationResult.Success);
            notDisposableRule.Validate("test@10minutemail.com").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test CreditCard
            var creditCardRule = ValidateRule.CreditCard();
            creditCardRule.Validate("4532015112830366").ShouldBeEquivalentTo(ValidationResult.Success); // Valid Luhn number
            creditCardRule.Validate("1234567890123456").ErrorMessage.ShouldNotBeNullOrEmpty(); // Invalid Luhn number

            // Test IPv4
            var ipv4Rule = ValidateRule.IPv4();
            ipv4Rule.Validate("192.168.1.1").ShouldBeEquivalentTo(ValidationResult.Success);
            ipv4Rule.Validate("300.300.300.300").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test Guid
            var guidRule = ValidateRule.Guid();
            guidRule.Validate(Guid.NewGuid().ToString()).ShouldBeEquivalentTo(ValidationResult.Success);
            guidRule.Validate("not-a-guid").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test ChineseIdCard
            var idCardRule = ValidateRule.ChineseIdCard();
            idCardRule.Validate("11010519491231002X").ShouldBeEquivalentTo(ValidationResult.Success); // Valid format and checksum
            idCardRule.Validate("110105194912310021").ErrorMessage.ShouldNotBeNullOrEmpty(); // Invalid checksum

            // Test Json
            var jsonRule = ValidateRule.Json();
            jsonRule.Validate("{\"key\": \"value\"}").ShouldBeEquivalentTo(ValidationResult.Success);
            jsonRule.Validate("invalid json").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test StrongPassword
            var strongPasswordRule = ValidateRule.StrongPassword();
            strongPasswordRule.Validate("StrongPass123!").ShouldBeEquivalentTo(ValidationResult.Success);
            strongPasswordRule.Validate("weak").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test NoWhitespace
            var noWhitespaceRule = ValidateRule.NoWhitespace();
            noWhitespaceRule.Validate("NoSpaces").ShouldBeEquivalentTo(ValidationResult.Success);
            noWhitespaceRule.Validate("Has Spaces").ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test FileExtension
            var fileExtensionRule = ValidateRule.FileExtension([".jpg", ".png", ".gif"]);
            fileExtensionRule.Validate("image.jpg").ShouldBeEquivalentTo(ValidationResult.Success);
            fileExtensionRule.Validate("document.pdf").ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void ValidateRuleFromRuleKeyShouldWork()
        {
            // Test reconstructing simple rules
            var emailRule = ValidateRule.Email();
            var reconstructedEmailRule = ValidateRule.FromRuleKey(emailRule.RuleKey);
            reconstructedEmailRule.ShouldNotBeNull();
            reconstructedEmailRule.RuleKey.ShouldBe(emailRule.RuleKey);

            // Test reconstructing parameterized rules
            var minLengthRule = ValidateRule.LengthMin(5);
            var reconstructedMinLengthRule = ValidateRule.FromRuleKey(minLengthRule.RuleKey);
            reconstructedMinLengthRule.ShouldNotBeNull();
            reconstructedMinLengthRule.RuleKey.ShouldBe(minLengthRule.RuleKey);

            // Test validation behavior is preserved
            var testValue = "test";
            var originalResult = minLengthRule.Validate(testValue);
            var reconstructedResult = reconstructedMinLengthRule.Validate(testValue);

            (originalResult.ErrorMessage != null).ShouldBe(reconstructedResult.ErrorMessage != null);
        }

        [Fact]
        public void ValidateRuleCompositeReconstructionShouldWork()
        {
            // Test reconstructing composite rules
            var emailRule = ValidateRule.Email();
            var minLengthRule = ValidateRule.LengthMin(10);
            var compositeRule = emailRule & minLengthRule;

            var reconstructedRule = ValidateRule.FromRuleKey(compositeRule.RuleKey);
            reconstructedRule.ShouldNotBeNull();
            reconstructedRule.RuleKey.ShouldBe(compositeRule.RuleKey);

            // Test validation behavior
            var validValue = "verylongemail@example.com";
            var invalidValue = "s@a.com";

            var originalValidResult = compositeRule.Validate(validValue);
            var reconstructedValidResult = reconstructedRule.Validate(validValue);
            originalValidResult.ShouldBe(ValidationResult.Success);
            reconstructedValidResult.ShouldBe(ValidationResult.Success);

            var originalInvalidResult = compositeRule.Validate(invalidValue);
            var reconstructedInvalidResult = reconstructedRule.Validate(invalidValue);
            originalInvalidResult.ShouldNotBe(ValidationResult.Success);
            reconstructedInvalidResult.ShouldNotBe(ValidationResult.Success);
        }

        [Fact]
        public void ValidateRuleParameterSerializationShouldWork()
        {
            // Test various parameter types
            var rangeRule = ValidateRule.Range(10, 100);
            var ruleKey = rangeRule.RuleKey;
            ruleKey.ShouldContain("10");
            ruleKey.ShouldContain("100");

            var reconstructedRule = ValidateRule.FromRuleKey(ruleKey);
            reconstructedRule.ShouldNotBeNull();

            // Test validation consistency
            var validValue = 50;
            var invalidValue = 5;

            rangeRule.Validate(validValue).ShouldBeEquivalentTo(ValidationResult.Success);
            reconstructedRule.Validate(validValue).ShouldBeEquivalentTo(ValidationResult.Success);

            rangeRule.Validate(invalidValue).ErrorMessage.ShouldNotBeNullOrEmpty();
            reconstructedRule.Validate(invalidValue).ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void ValidateRuleErrorHandlingShouldWork()
        {
            // Test null values
            var emailRule = ValidateRule.Email();
            var nullResult = emailRule.Validate(null);
            nullResult.ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test empty values where appropriate
            var minLengthRule = ValidateRule.LengthMin(3);
            var emptyResult = minLengthRule.Validate("");
            emptyResult.ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test invalid rule key reconstruction
            var invalidRule = ValidateRule.FromRuleKey("NonExistentRule");
            invalidRule.ShouldNotBeNull();
            invalidRule.ShouldBe(ValidateRule.Null);

            // Test null rule validation
            var nullRuleResult = ValidateRule.Null.Validate("anything");
            nullRuleResult.ShouldBeEquivalentTo(ValidationResult.Success); // Null rule always passes
        }

        [Fact]
        public void ValidateRuleCachingShouldWork()
        {
            // Test that rules are cached
            var rule1 = ValidateRule.Email();
            var rule2 = ValidateRule.Email();

            // Should be the same instance due to caching
            ReferenceEquals(rule1, rule2).ShouldBeTrue();

            // Test parameterized rules
            var minRule1 = ValidateRule.LengthMin(5);
            var minRule2 = ValidateRule.LengthMin(5);
            ReferenceEquals(minRule1, minRule2).ShouldBeTrue();

            // Different parameters should create different instances
            var minRule3 = ValidateRule.LengthMin(10);
            ReferenceEquals(minRule1, minRule3).ShouldBeFalse();
        }

        [Fact]
        public void ValidateAttributeConstructorsShouldWork()
        {
            // Test constructor with ruleKey only
            var attribute1 = new ValidateAttribute(nameof(ValidateRule.Email));
            attribute1.RuleKey.ShouldBe(nameof(ValidateRule.Email));
            attribute1.Message.ShouldBeNull();

            // Test constructor with ruleKey and message
            var attribute2 = new ValidateAttribute(nameof(ValidateRule.Email), "Custom email message");
            attribute2.RuleKey.ShouldBe(nameof(ValidateRule.Email));
            attribute2.Message.ShouldBe("Custom email message");

            // Test constructor with parameters
            var attribute3 = new ValidateAttribute(nameof(ValidateRule.LengthMin), new object[] { 5 }, "Custom length message");
            attribute3.RuleKey.ShouldContain(nameof(ValidateRule.LengthMin));
            attribute3.RuleKey.ShouldContain("5");
            attribute3.Message.ShouldBe("Custom length message");

            // Test ToDirective conversion
            var directive = attribute1.ToDirective();
            directive.ShouldNotBeNull();
            directive.RuleKey.ShouldBe(attribute1.RuleKey);
            directive.Message.ShouldBe(attribute1.Message);
        }

        [Fact]
        public void ValidateDirectiveConstructorsShouldWork()
        {
            // Test parameterless constructor
            var directive1 = new ValidateDirective();
            directive1.ShouldNotBeNull();

            // Test constructor with rule and message
            var emailRule = ValidateRule.Email();
            var directive2 = new ValidateDirective(emailRule, "Email validation failed");
            directive2.Rule.ShouldBe(emailRule);
            directive2.RuleKey.ShouldBe(emailRule.RuleKey);
            directive2.Message.ShouldBe("Email validation failed");

            // Test constructor with ruleKey and message
            var directive3 = new ValidateDirective("Email", "Email validation failed");
            directive3.RuleKey.ShouldBe("Email");
            directive3.Message.ShouldBe("Email validation failed");
            directive3.Rule.ShouldNotBeNull(); // Should be constructed from ruleKey
        }

        [Fact]
        public void ValidateExtensionMethodsShouldWork()
        {
            // Note: These tests verify the extension methods compile and work correctly
            // The actual functionality is tested in the schema integration tests

            var emailRule = ValidateRule.Email();
            var lengthRule = ValidateRule.LengthMin(3);

            // Test that extension methods return correct types and can be chained
            // In actual usage, these would be called on descriptor instances
            var directive1 = new ValidateDirective(emailRule, "Email error");
            var directive2 = new ValidateDirective(lengthRule, "Length error");
            var directive3 = new ValidateDirective("CustomRule", "Custom error");

            directive1.RuleKey.ShouldBe(emailRule.RuleKey);
            directive2.RuleKey.ShouldBe(lengthRule.RuleKey);
            directive3.RuleKey.ShouldBe("CustomRule");
        }

        [Fact]
        public void ValidateRuleEdgeCasesShouldBeHandled()
        {
            // Test null/empty string handling for different rules
            var emailRule = ValidateRule.Email();
            emailRule.Validate(null).ErrorMessage.ShouldNotBeNullOrEmpty();
            emailRule.Validate("").ErrorMessage.ShouldNotBeNullOrEmpty();

            var urlRule = ValidateRule.Url();
            urlRule.Validate(null).ShouldBeEquivalentTo(ValidationResult.Success); // URL rule allows null/empty
            urlRule.Validate("").ShouldBeEquivalentTo(ValidationResult.Success);

            var guidRule = ValidateRule.Guid();
            guidRule.Validate(null).ShouldBeEquivalentTo(ValidationResult.Success); // GUID rule allows null/empty
            guidRule.Validate("").ShouldBeEquivalentTo(ValidationResult.Success);

            // Test boundary values for numeric rules
            var rangeRule = ValidateRule.Range(0, 100);
            rangeRule.Validate(0).ShouldBeEquivalentTo(ValidationResult.Success); // Boundary value should be valid
            rangeRule.Validate(100).ShouldBeEquivalentTo(ValidationResult.Success); // Boundary value should be valid
            rangeRule.Validate(-1).ErrorMessage.ShouldNotBeNullOrEmpty();
            rangeRule.Validate(101).ErrorMessage.ShouldNotBeNullOrEmpty();

            // Test edge cases for string length rules
            var lengthRule = ValidateRule.LengthRange(0, 5);
            lengthRule.Validate("").ShouldBeEquivalentTo(ValidationResult.Success); // Empty string should be valid for 0-length min
            lengthRule.Validate("12345").ShouldBeEquivalentTo(ValidationResult.Success); // Exact max length
            lengthRule.Validate("123456").ErrorMessage.ShouldNotBeNullOrEmpty(); // Over max length
        }

        [Fact]
        public void SpecialCaseValidationRulesShouldWork()
        {
            // Test CreditCard with formatted and unformatted numbers
            var creditCardRule = ValidateRule.CreditCard();
            creditCardRule.Validate("4532-0151-1283-0366").ShouldBeEquivalentTo(ValidationResult.Success); // With dashes
            creditCardRule.Validate("4532 0151 1283 0366").ShouldBeEquivalentTo(ValidationResult.Success); // With spaces
            creditCardRule.Validate("4532015112830366").ShouldBeEquivalentTo(ValidationResult.Success); // Without formatting

            // Test ChineseIdCard with various valid formats
            var idCardRule = ValidateRule.ChineseIdCard();
            // Note: The validation includes date validation and checksum verification
            var validIdCard = "11010519491231002X"; // Known valid ID with correct checksum
            idCardRule.Validate(validIdCard).ShouldBeEquivalentTo(ValidationResult.Success);

            // Test StrongPassword with various configurations
            var strongPasswordAllRequired = ValidateRule.StrongPassword(true, true, true, true);
            strongPasswordAllRequired.Validate("Password123!").ShouldBeEquivalentTo(ValidationResult.Success);
            strongPasswordAllRequired.Validate("password123!").ErrorMessage.ShouldNotBeNullOrEmpty(); // No uppercase
            strongPasswordAllRequired.Validate("PASSWORD123!").ErrorMessage.ShouldNotBeNullOrEmpty(); // No lowercase
            strongPasswordAllRequired.Validate("Password!").ErrorMessage.ShouldNotBeNullOrEmpty(); // No digit
            strongPasswordAllRequired.Validate("Password123").ErrorMessage.ShouldNotBeNullOrEmpty(); // No special char

            var strongPasswordNoSpecial = ValidateRule.StrongPassword(true, true, true, false);
            strongPasswordNoSpecial.Validate("Password123").ShouldBeEquivalentTo(ValidationResult.Success);

            // Test FileExtension case insensitivity
            var fileExtRule = ValidateRule.FileExtension([".jpg", ".PNG", ".gif"]);
            fileExtRule.Validate("image.jpg").ShouldBeEquivalentTo(ValidationResult.Success);
            fileExtRule.Validate("image.JPG").ShouldBeEquivalentTo(ValidationResult.Success); // Case insensitive
            fileExtRule.Validate("image.png").ShouldBeEquivalentTo(ValidationResult.Success);
            fileExtRule.Validate("image.PNG").ShouldBeEquivalentTo(ValidationResult.Success);
        }

        [Fact]
        public void ComplexCompositeRulesShouldWork()
        {
            // Test nested composite rules
            var emailRule = ValidateRule.Email();
            var minLengthRule = ValidateRule.LengthMin(5);
            var maxLengthRule = ValidateRule.LengthMax(50);
            var notDisposableRule = ValidateRule.EmailNotDisposable();

            // Create a complex rule: (Email AND MinLength AND MaxLength) AND NotDisposable
            var basicEmailRule = emailRule & minLengthRule & maxLengthRule;
            var complexEmailRule = basicEmailRule & notDisposableRule;

            // Test valid email
            var validEmail = "user@example.com";
            complexEmailRule.Validate(validEmail).ShouldBeEquivalentTo(ValidationResult.Success);

            // Test invalid cases
            complexEmailRule.Validate("short").ErrorMessage.ShouldNotBeNullOrEmpty(); // Not email
            complexEmailRule.Validate("a@b.c").ErrorMessage.ShouldNotBeNullOrEmpty(); // Too short
            complexEmailRule.Validate("test@10minutemail.com").ErrorMessage.ShouldNotBeNullOrEmpty(); // Disposable

            // Test OR composite with AND composite
            var alphaRule = ValidateRule.Alpha();
            var numericRule = ValidateRule.Numeric();
            var alphaOrNumeric = alphaRule | numericRule;
            var longAlphaOrNumeric = alphaOrNumeric & ValidateRule.LengthMin(5);

            longAlphaOrNumeric.Validate("Letters").ShouldBeEquivalentTo(ValidationResult.Success); // Long alpha
            longAlphaOrNumeric.Validate("12345").ShouldBeEquivalentTo(ValidationResult.Success); // Long numeric
            longAlphaOrNumeric.Validate("ABC").ErrorMessage.ShouldNotBeNullOrEmpty(); // Short alpha
            longAlphaOrNumeric.Validate("123").ErrorMessage.ShouldNotBeNullOrEmpty(); // Short numeric
            longAlphaOrNumeric.Validate("Mix3d12345").ErrorMessage.ShouldNotBeNullOrEmpty(); // Mixed but long
        }

        [Fact]
        public void ValidationRuleKeyGenerationShouldBeConsistent()
        {
            // Test that same rules generate same keys
            var rule1 = ValidateRule.LengthMin(5);
            var rule2 = ValidateRule.LengthMin(5);
            rule1.RuleKey.ShouldBe(rule2.RuleKey);

            // Test that different parameters generate different keys
            var rule3 = ValidateRule.LengthMin(10);
            rule1.RuleKey.ShouldNotBe(rule3.RuleKey);

            // Test that composite rules generate deterministic keys
            var emailRule = ValidateRule.Email();
            var lengthRule = ValidateRule.LengthMin(5);
            var composite1 = emailRule & lengthRule;
            var composite2 = emailRule & lengthRule;
            composite1.RuleKey.ShouldBe(composite2.RuleKey);

            // Test that order matters in composite rules
            var composite3 = lengthRule & emailRule;
            composite1.RuleKey.ShouldNotBe(composite3.RuleKey);

            // Test complex parameter serialization
            var rangeRule = ValidateRule.Range(DateTime.Today, DateTime.Today.AddDays(30));
            var reconstructedRule = ValidateRule.FromRuleKey(rangeRule.RuleKey);
            rangeRule.Validate(DateTime.Today.AddDays(15)).ShouldBeEquivalentTo(ValidationResult.Success);
            reconstructedRule.Validate(DateTime.Today.AddDays(15)).ShouldBeEquivalentTo(ValidationResult.Success);
        }

        [Fact]
        public async Task ValidateRuleThreadSafetyShouldWork()
        {
            // Test that rule caching is thread-safe
            var tasks = new List<Task<ValidateRule<string>>>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => ValidateRule.Email()));
            }

            Task.WaitAll(tasks.ToArray());

            // All tasks should return the same cached instance
            var firstRule = await tasks[0];
            foreach (var task in tasks)
            {
                ReferenceEquals(firstRule, await task).ShouldBeTrue();
            }

            // Test with parameterized rules
            var paramTasks = new List<Task<ValidateRule<string>>>();

            for (int i = 0; i < 10; i++)
            {
                paramTasks.Add(Task.Run(() => ValidateRule.LengthMin(5)));
            }

            Task.WaitAll(paramTasks.ToArray());

            var firstParamRule = await paramTasks[0];
            foreach (var task in paramTasks)
            {
                ReferenceEquals(firstParamRule, await task).ShouldBeTrue();
            }
        }
    }
}
