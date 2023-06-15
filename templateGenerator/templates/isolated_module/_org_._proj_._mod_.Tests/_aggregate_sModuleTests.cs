using System;
using System.Linq;
using System.Threading.Tasks;

using AutoFixture;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Testing;

using HotChocolate.Execution;

using _org_._proj_._mod_.Api.Aggregates._aggregate_s;
using _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs;
using _org_._proj_._mod_.Core;
using _org_._proj_._mod_.Core.Aggregates._aggregate_s;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using MongoDB.Bson;
using MongoDB.Entities;

using Shouldly;

using Volo.Abp;

using Xunit;

namespace _org_._proj_._mod_.Tests
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
                 var result = await this._mediator.Send(request);
                 result.Name.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
                 //result.Code.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
             });

            // 副作用操作校验结果
            var check = await GetRequiredService<DbContext>().Find<_aggregate_>().Match(x => x.Name == nameof(Create_aggregate_Request_Should_Work)).ExecuteSingleAsync();
            check.Name.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
            //check.Code.ShouldBe(nameof(Create_aggregate_Request_Should_Work));
        }

        [Fact]
        public async Task QueryInput_Should_Work()
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var result = await _mediator.Send(new QueryInput<I_aggregate_>());
                 result.Count().ShouldBeGreaterThan(0);
             });
        }
    }
}
