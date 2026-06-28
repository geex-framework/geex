using System.Linq;

using Geex.Tests.FeatureTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class CoreBatchLoadServiceTests : TestsBase
    {
        private const string ProfilerNamespaceFilter = "BatchLoadTest";

        public CoreBatchLoadServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ManualBatchLoadShouldBatchLoadChildrenAndFirstChild()
        {
            await SeedBatchLoadDataAsync();

            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await DB.RestartProfiler();

            var result = uow.Query<BatchLoadTestEntity>()
                .BatchLoad(x => x.Children)
                .BatchLoad(x => x.FirstChild)
                .ToList();

            result.Count.ShouldBe(2);
            result.TrueForAll(x => x.Children.All(child => child.ParentId == x.ThisId)).ShouldBeTrue();
            result.Select(x => x.FirstChild).Count(x => x?.Value != default).ShouldBe(2);

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains("BatchLoadTest"));
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ManualBatchLoadWithoutBatchLoadShouldStillReturnNestedData()
        {
            await SeedBatchLoadDataAsync();

            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            var result = uow.Query<BatchLoadTestEntity>().ToList();

            result.Count.ShouldBe(2);
            result.First(x => x.ThisId == "1").Children.Count().ShouldBe(2);
            result.First(x => x.ThisId == "1").FirstChild.Value!.ThisId.ShouldBe("1.1");
        }

        [Fact]
        public async Task ManualNestedBatchLoadShouldLoadThreeLevels()
        {
            await SeedBatchLoadDataAsync();

            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await DB.RestartProfiler();

            var result = uow.Query<BatchLoadTestEntity>()
                .BatchLoad(x => x.Children)
                    .ThenBatchLoad(x => x.FirstChild)
                .ToList();

            result.First(x => x.ThisId == "1")
                .Children.First(x => x.ThisId == "1.1")
                .FirstChild.Value!.ThisId.ShouldBe("1.1.1");

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        private async Task SeedBatchLoadDataAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<BatchLoadTestEntity>(_ => true);
            await uow.DeleteAsync<BatchLoadTestChildEntity>(_ => true);
            await uow.DeleteAsync<BatchLoadTestGrandChildEntity>(_ => true);

            uow.Attach(new BatchLoadTestEntity("1"));
            uow.Attach(new BatchLoadTestEntity("2"));
            uow.Attach(new BatchLoadTestChildEntity("1.1", "1"));
            uow.Attach(new BatchLoadTestChildEntity("1.2", "1"));
            uow.Attach(new BatchLoadTestChildEntity("2.1", "2"));
            uow.Attach(new BatchLoadTestGrandChildEntity("1.1.1", "1.1"));
            await uow.SaveChanges();
        }
    }
}
