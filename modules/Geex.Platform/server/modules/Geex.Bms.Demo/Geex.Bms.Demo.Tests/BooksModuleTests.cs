using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Testing;
using Geex.Bms.Demo.Core;
using Geex.Bms.Demo.Core.Aggregates.Books;
using Geex.Bms.Demo.Core.GqlSchemas.Books;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;

using Shouldly;
using Xunit;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;

namespace Geex.Bms.Demo.Tests
{
    public class demoModuleTests : ModuleTestBase<bmsdemoCoreModule>
    {
        private readonly IMediator _mediator;

        public demoModuleTests()
        {
            _mediator = GetRequiredService<IMediator>();
        }

        public class CreatebookRequestTestData : TheoryData<CreatebookRequest>
        {
            public CreatebookRequestTestData()
            {
                Add(new CreatebookRequest()
                {
                    Name = nameof(CreatebookRequest_Should_Work),
                    //Code = nameof(CreatebookRequest_Should_Work),
                });
            }
        }
        [Theory]
        [ClassData(typeof(CreatebookRequestTestData))]
        public async Task CreatebookRequest_Should_Work(CreatebookRequest request)
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var mutation = ServiceProvider.GetRequiredService<bookMutation>();
                 var result = await mutation.Createbook(request);
                 result.Name.ShouldBe(nameof(CreatebookRequest_Should_Work));
                 //result.Code.ShouldBe(nameof(CreatebookRequest_Should_Work));
             });

            // 副作用操作校验结果
            var check = await GetRequiredService<DbContext>().Find<Book>().Match(x => x.Name == nameof(CreatebookRequest_Should_Work)).ExecuteSingleAsync();
            check.Name.ShouldBe(nameof(CreatebookRequest_Should_Work));
            //check.Code.ShouldBe(nameof(CreatebookRequest_Should_Work));
        }

        [Fact]
        public async Task QueryInput_Should_Work()
        {
            // 测试功能
            await base.WithUow(async () =>
             {
                 var query = ServiceProvider.GetRequiredService<bookQuery>();
                 var result = await query.books(new QuerybookRequest());
                 result.Count().ShouldBeGreaterThan(0);
             });
        }
    }
}
