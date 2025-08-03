using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Validation;
using Geex.Tests.SchemaTests.TestEntities;
using Geex.Gql.Types;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    /// <summary>
    /// 测试类: 用于测试Validation功能的API测试
    /// 包括基于Attribute和动态Configure的两种验证方式
    /// </summary>
    [Collection(nameof(TestsCollection))]
    public class ValidationApiTests : TestsBase
    {
        public ValidationApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryWithAttributeBasedValidation_ShouldWork()
        {
            // Arrange - 使用有效的参数
            var client = this.SuperAdminClient;
            var query = """
                query($email: String!, $name: String!) {
                    validateTestQueryField(email: $email, name: $name) {
                        id
                        validatedField
                        emailField
                        ageField
                        __typename
                    }
                }
                """;

            var validEmail = "test@example.com";
            var validName = "TestUser";

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { email = validEmail, name = validName }, true);

            // Assert
            responseData["data"].ShouldNotBeNull();
        }

        [Fact]
        public async Task QueryWithAttributeBasedValidation_InvalidEmail_ShouldFail()
        {
            // Arrange - 使用无效的邮箱
            var client = this.SuperAdminClient;
            var query = """
                query($email: String!, $name: String!) {
                    validateTestQueryField(email: $email, name: $name) {
                        id
                        validatedField
                        emailField
                        ageField
                        __typename
                    }
                }
                """;

            var invalidEmail = "invalid-email";
            var validName = "TestUser";

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { email = invalidEmail, name = validName }, true);

            // Assert - 应该出现验证错误
            responseData["errors"].ShouldNotBeNull();
            var error = responseData["errors"][0];
            ((string)error["extensions"]["code"]).ShouldBe(ValidateRule.ValidationErrorCode);
            ((string)error["message"]).ShouldContain("Invalid email format");
        }

        [Fact]
        public async Task QueryWithAttributeBasedValidation_ShortName_ShouldFail()
        {
            // Arrange - 使用太短的姓名
            var client = this.SuperAdminClient;
            var query = """
                query($email: String!, $name: String!) {
                    validateTestQueryField(email: $email, name: $name) {
                        id
                        validatedField
                        emailField
                        ageField
                        __typename
                    }
                }
                """;

            var validEmail = "test@example.com";
            var shortName = "Ab"; // 长度小于3

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { email = validEmail, name = shortName }, true);

            // Assert - 应该出现验证错误
            responseData["errors"].ShouldNotBeNull();
            var error = responseData["errors"][0];
            ((string)error["extensions"]["code"]).ShouldBe(ValidateRule.ValidationErrorCode);
            ((string)error["message"]).ShouldContain("Name must be at least 3 characters");
        }

        [Fact]
        public async Task MutationWithAttributeBasedValidation_ShouldWork()
        {
            // Arrange - 使用有效的参数
            var client = this.SuperAdminClient;
            var query = """
                mutation($name: String!, $count: Int!) {
                    validateTestMutationField(name: $name, count: $count)
                }
                """;

            var validName = "TestUser";
            var validCount = 50;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { name = validName, count = validCount }, true);

            // Assert
            responseData["data"].ShouldNotBeNull();
        }

        [Fact]
        public async Task MutationWithAttributeBasedValidation_InvalidCount_ShouldFail()
        {
            // Arrange - 使用超出范围的计数
            var client = this.SuperAdminClient;
            var query = """
                mutation($name: String!, $count: Int!) {
                    validateTestMutationField(name: $name, count: $count)
                }
                """;

            var validName = "TestUser";
            var invalidCount = 150; // 超出范围(1-100)

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { name = validName, count = invalidCount }, true);

            // Assert - 应该出现验证错误
            responseData["errors"].ShouldNotBeNull();
            var error = responseData["errors"][0];
            ((string)error["extensions"]["code"]).ShouldBe(ValidateRule.ValidationErrorCode);
            ((string)error["message"]).ShouldContain("Count must be between 1 and 100");
        }

        [Fact]
        public async Task ValidationRulesFromSchema_ShouldBeCorrectlyConfigured()
        {
            // Arrange & Act - 通过服务验证Schema配置
            using var scope = ScopedService.CreateScope();
            var schema = scope.ServiceProvider.GetService<HotChocolate.ISchema>();

            // Assert - 验证Schema中包含验证指令
            schema.ShouldNotBeNull();

            var queryType = schema.GetType<ObjectType>("Query");
            queryType.ShouldNotBeNull();

            var validateTestField = queryType.Fields.FirstOrDefault(f => f.Name == "validateTestQueryField");
            validateTestField.ShouldNotBeNull();

            // 验证参数上是否有验证指令
            var emailArg = validateTestField.Arguments.FirstOrDefault(a => a.Name == "email");
            emailArg.ShouldNotBeNull();
            var hasValidationDirective = emailArg.Directives.Any(d => d.Type.Name == ValidateDirective.DirectiveName);
            hasValidationDirective.ShouldBeTrue("Email argument should have validation directive");
        }
    }
}
