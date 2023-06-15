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
    public class x_Mod_xModuleTests : ModuleTestBase<x_Proj_xx_Mod_xCoreModule>
    {
        private readonly IMediator _mediator;

        public x_Mod_xModuleTests()
        {
            _mediator = GetRequiredService<IMediator>();
        }

        public class Createx_Aggregate_xRequestTestData : TheoryData<Createx_Aggregate_xInput>
        {
            public Createx_Aggregate_xRequestTestData()
            {
                Add(new Createx_Aggregate_xInput()
                {
                    Name = nameof(Createx_Aggregate_xRequest_Should_Work),
                    //Code = nameof(Createx_Aggregate_xRequest_Should_Work),
                });
            }
        }
        [Theory]
        [ClassData(typeof(Createx_Aggregate_xRequestTestData))]
        public async Task Createx_Aggregate_xRequest_Should_Work(Createx_Aggregate_xInput input)
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var mutation = ServiceProvider.GetRequiredService<x_Aggregate_xMutation>();
                 var result = await mutation.Createx_Aggregate_x(input);
                 result.Name.ShouldBe(nameof(Createx_Aggregate_xRequest_Should_Work));
                 //result.Code.ShouldBe(nameof(Createx_Aggregate_xRequest_Should_Work));
             });

            // 副作用操作校验结果
            var check = await GetRequiredService<DbContext>().Find<x_Aggregate_x>().Match(x => x.Name == nameof(Createx_Aggregate_xRequest_Should_Work)).ExecuteSingleAsync();
            check.Name.ShouldBe(nameof(Createx_Aggregate_xRequest_Should_Work));
            //check.Code.ShouldBe(nameof(Createx_Aggregate_xRequest_Should_Work));
        }

        [Fact]
        public async Task QueryInput_Should_Work()
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var query = ServiceProvider.GetRequiredService<x_Aggregate_xQuery>();
                 var result = await query.x_Aggregate_xs(new Queryx_Aggregate_xInput());
                 result.Count().ShouldBeGreaterThan(0);
             });
        }
    }
}
