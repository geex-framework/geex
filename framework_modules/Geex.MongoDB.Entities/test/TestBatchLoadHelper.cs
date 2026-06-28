using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Driver;
using MongoDB.Entities;
using MongoDB.Entities.Tests.Models;
using MongoDB.Entities.Utilities;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestBatchLoadHelper
    {
        [TestMethod]
        public async Task merge_config_should_register_property_tree()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteTypedAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();

            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var config = new BatchLoadConfig();
            config.EnsurePath([childrenProperty]);

            var query = dbContext.Query<BatchLoadEntity>();
            query.MergeBatchLoadConfig(config);

            await DB.RestartProfiler();
            var result = query.ToList();
            result.First(x => x.ThisId == "1").Children.Count().ShouldBe(1);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(2);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task merge_config_should_skip_when_manual_batchload_exists()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteTypedAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();

            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;
            var config = new BatchLoadConfig();
            config.EnsurePath([childrenProperty]);

            var query = dbContext.Query<BatchLoadEntity>().BatchLoad(x => x.FirstChild);
            query.MergeBatchLoadConfig(config);

            var provider = (CachedDbContextQueryProvider<BatchLoadEntity>)query.Provider;
            provider.BatchLoadConfig.ContainsSubConfig(firstChildProperty).ShouldBeTrue();
            provider.BatchLoadConfig.ContainsSubConfig(childrenProperty).ShouldBeFalse();

            await DB.RestartProfiler();
            var result = query.ToList();
            result.First(x => x.ThisId == "1").FirstChild.Value.ShouldNotBeNull();
            result.First(x => x.ThisId == "1").Children.Count().ShouldBe(1);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task load_entities_should_batch_load_single_entity_collection()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteTypedAsync<BatchLoadEntity>();
            var root = new BatchLoadEntity(thisId: "1");
            dbContext.Attach(root);
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            root = dbContext.Query<BatchLoadEntity>().First(x => x.ThisId == "1");

            var config = new BatchLoadConfig();
            config.EnsurePath([typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!]);

            await DB.RestartProfiler();
            root.LoadBatchLoad(config);
            root.Children.Count().ShouldBe(1);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(1);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task load_entities_should_support_nested_then_batchload()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteTypedAsync<RootEntity>();
            await dbContext.DeleteTypedAsync<RootEntity.C1Entity>();
            await dbContext.DeleteTypedAsync<RootEntity.C2Entity>();
            dbContext.Attach(new RootEntity(thisId: "1"));
            dbContext.Attach(new RootEntity.C1Entity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new RootEntity.C2Entity(thisId: "1.1.1", parentId: "1.1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();

            var root = dbContext.Query<RootEntity>().First();
            var config = new BatchLoadConfig();
            var c1Property = typeof(RootEntity).GetProperty(nameof(RootEntity.C1))!;
            var c2Property = typeof(RootEntity.C1Entity).GetProperty(nameof(RootEntity.C1Entity.C2))!;
            var c1Config = config.GetOrAddSubConfig(c1Property);
            c1Config.GetOrAddSubConfig(c2Property);

            await DB.RestartProfiler();
            root.LoadBatchLoad(config);
            root.C1.First().C2.Count().ShouldBe(1);
            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns == "mongodb-entities-test.RootEntity"
                            || x.ns == "mongodb-entities-test.C1Entity"
                            || x.ns == "mongodb-entities-test.C2Entity");
            logs.Count().ShouldBe(2);
            DB.StopProfiler();
        }

        [TestMethod]
        public void merge_config_should_throw_for_non_lazyquery_property()
        {
            var dbContext = new DbContext();
            var query = dbContext.Query<BatchLoadEntity>();
            var config = new BatchLoadConfig();
            config.EnsurePath([typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.ThisId))!]);

            Should.NotThrow(() => query.MergeBatchLoadConfig(config));
            Should.Throw<BatchLoadConfigurationException>(() => query.ToList())
                .PropertyName.ShouldBe(nameof(BatchLoadEntity.ThisId));
        }
    }
}
