using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Testing;
using x_Org_x.x_Proj_x.x_Mod_x.Core;
using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;
using x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;

using Shouldly;
using Xunit;
using x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs.Inputs;

namespace x_Org_x.x_Proj_x.x_Mod_x.Tests
{
    public class _mod_ModuleTests : ModuleTestBase<_proj__mod_CoreModule>
    {
        private readonly IMediator _mediator;

        public _mod_ModuleTests()
        {
            _mediator = GetRequiredService<IMediator>();
        }

        public class Create_aggregate_RequestTestData : TheoryData<Create_aggregate_Request>
        {
            public Create_aggregate_RequestTestData()
            {
                Add(new Create_aggregate_Request()
                {
                    Name = nameof(Create_aggregate_Request_Should_Work),
                    //Code = nameof(Create_aggregate_Request_Should_Work),
                });
            }
        }
        [Theory]
        [ClassData(typeof(Create_aggregate_RequestTestData))]
        public async Task Create_aggregate_Request_Should_Work(Create_aggregate_Request request)
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var mutation = ServiceProvider.GetRequiredService<_aggregate_Mutation>();
                 var result = await mutation.Create_aggregate_(request);
                 result.Name.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
                 //result.Code.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
             });

            // 副作用操作校验结果
            var check = await GetRequiredService<DbContext>().Find<x_Aggregate_x>().Match(x => x.Name == nameof(Create_aggregate_Request_Should_Work)).ExecuteSingleAsync();
            check.Name.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
            //check.Code.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
        }

        [Fact]
        public async Task QueryInput_Should_Work()
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var query = ServiceProvider.GetRequiredService<_aggregate_Query>();
                 var result = await query._aggregate_s(new Query_aggregate_Request());
                 result.Count().ShouldBeGreaterThan(0);
             });
        }
    }
}
