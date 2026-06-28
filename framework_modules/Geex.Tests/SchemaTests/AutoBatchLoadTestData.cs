using Geex.Tests.SchemaTests.TestEntities;

using MongoDB.Entities;

namespace Geex.Tests.SchemaTests;

internal static class AutoBatchLoadTestData
{
    public static async Task SeedSingleTreeAsync(IUnitOfWork uow)
    {
        await uow.DeleteAsync<AutoBatchLoadTestEntity>(_ => true);
        await uow.DeleteAsync<AutoBatchLoadChildEntity>(_ => true);
        await uow.DeleteAsync<AutoBatchLoadGrandChildEntity>(_ => true);

        uow.Attach(new AutoBatchLoadTestEntity("1"));
        uow.Attach(new AutoBatchLoadChildEntity("1.1", "1"));
        uow.Attach(new AutoBatchLoadGrandChildEntity("1.1.1", "1.1"));
        await uow.SaveChanges();
    }

    public static async Task SeedMultiChildTreeAsync(IUnitOfWork uow)
    {
        await uow.DeleteAsync<AutoBatchLoadTestEntity>(_ => true);
        await uow.DeleteAsync<AutoBatchLoadChildEntity>(_ => true);
        await uow.DeleteAsync<AutoBatchLoadGrandChildEntity>(_ => true);

        uow.Attach(new AutoBatchLoadTestEntity("1"));
        uow.Attach(new AutoBatchLoadChildEntity("1.1", "1"));
        uow.Attach(new AutoBatchLoadChildEntity("1.2", "1"));
        uow.Attach(new AutoBatchLoadGrandChildEntity("1.1.1", "1.1"));
        uow.Attach(new AutoBatchLoadGrandChildEntity("1.2.1", "1.2"));
        await uow.SaveChanges();
    }
}
