using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using MongoDB.Bson;
using Shouldly;
using Geex.Tests.SchemaTests.TestEntities;
using Geex.Extensions.ApprovalFlows;
using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;
using HotChocolate;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Tests.FeatureTests
{
    public class 中文嵌套类
    {
        public string 测试类名称 { get; set; }
        public static implicit operator 中文嵌套类(中文嵌套类输入参数 参数)
        {
            return new 中文嵌套类() { 测试类名称 = 参数.测试类名称 };
        }
    }
    public class 中文嵌套类输入参数
    {
        public string 测试类名称 { get; set; }
    }
    public class 测试枚举 : Enumeration<测试枚举>
    {
        public static 测试枚举 测试枚举值1 { get; set; } = 测试枚举.FromValue(nameof(测试枚举值1));
        public static 测试枚举 测试枚举值2 { get; set; } = 测试枚举.FromValue(nameof(测试枚举值2));
    }
    public class 中文测试实体 : Entity<中文测试实体>
    {
        public string 名称 { get; set; }
        public 测试枚举 测试枚举 { get; set; } = 测试枚举.测试枚举值1;
        public 中文嵌套类 测试类 { get; set; } = new 中文嵌套类();
        public class 中文测试实体GqlConfig : GqlConfig.Object<中文测试实体>
        {
            protected override void Configure(IObjectTypeDescriptor<中文测试实体> descriptor)
            {
                descriptor.ConfigEntity();
            }
        }
    }

    public class 中文测试Mutation : MutationExtension<中文测试Mutation>
    {
        private readonly IUnitOfWork _uow;

        public 中文测试Mutation(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public 中文测试实体 创建中文测试实体(string 名称, 测试枚举 测试枚举值, 中文嵌套类输入参数 测试类)
        {
            var 实体 = new 中文测试实体() { 名称 = 名称, 测试枚举 = 测试枚举值, 测试类 = 测试类 };
            _uow.Attach(实体);
            return 实体;
        }
    }

    [Collection(nameof(TestsCollection))]
    public class UnicodeApiTests : TestsBase
    {
        public UnicodeApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task UnicodeTestEntityFieldsShouldBeAdded()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Check if ApproveStatus field is configured for IApproveEntity types
            var approveEntityType = schema.GetType<ObjectType>(nameof(中文测试实体));
            approveEntityType.ShouldNotBeNull();
            approveEntityType.Fields.TryGetField("名称", out var 名称Field).ShouldBeTrue();
            approveEntityType.Fields.TryGetField("测试枚举", out var 测试枚举Field).ShouldBeTrue();
            approveEntityType.Fields.TryGetField("测试类", out var 测试类Field).ShouldBeTrue();

            using var scope = ScopedService.CreateScope();
            var client = this.SuperAdminClient;
            var query = """
                  mutation {
                      创建中文测试实体(名称: "测试", 测试枚举值: 测试枚举值1, 测试类: { 测试类名称: "测试类" }) {
                        id
                        名称
                        测试枚举
                        测试类 {
                          测试类名称
                        }
                      }
                  }
                  """;

            var (responseData, responseString) = await client.PostGqlRequest(query);

            var 创建中文测试实体 = responseData["data"]["创建中文测试实体"];
            创建中文测试实体["id"].GetValue<string>().ShouldNotBeNull();
            创建中文测试实体["名称"].GetValue<string>().ShouldBe("测试");
            创建中文测试实体["测试枚举"].GetValue<string>().ShouldBe("测试枚举值1");
            创建中文测试实体["测试类"]["测试类名称"].GetValue<string>().ShouldBe("测试类");
        }
    }
}
