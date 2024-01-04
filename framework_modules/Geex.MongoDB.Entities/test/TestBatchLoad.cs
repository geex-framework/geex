using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;
using MongoDB.Entities.Utilities;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestBatchLoad
    {
        [TestMethod]
        public async Task query_batch_load_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1.1", parentId: "1.1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<BatchLoadEntity>()
                .BatchLoad(x => x.Children)
                .BatchLoad(x => x.FirstChild)
                .ToList();
            result.TrueForAll(x => x.Children.All(child => child.ParentId == x.ThisId)).ShouldBe(true);
            result.Select(x => x.FirstChild).Where(x => x?.Value != default).ShouldNotBeEmpty();
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(3);
            result.Count.ShouldBe(3);
            result.SelectMany(x => x.Children).Count().ShouldBe(2);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task query_batch_load_should_work_with_single_item()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "2"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.2", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "2.1", parentId: "2"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<BatchLoadEntity>().BatchLoad(x => x.FirstChild).ToList();
            result.Select(x => x.FirstChild).Count(x => x?.Value != default).ShouldBe(2);
            result.Count(x => x.Children.Any()).ShouldBe(2);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(2 + 5);
            result.Count.ShouldBe(5);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task query_batch_load_should_work_with_no_prefetch()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "2"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.2", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "2.1", parentId: "2"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<BatchLoadEntity>().ToList();
            result.Select(x => x.FirstChild).Count(x => x.Value != default).ShouldBe(2);
            result.Count(x => x.Children.Any()).ShouldBe(2);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(1 + 5 + 5);
            result.Count.ShouldBe(5);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task query_batch_load_should_work_with_nested_batch_load()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<RootEntity>();
            await dbContext.DeleteAsync<RootEntity.C1Entity>();
            await dbContext.DeleteAsync<RootEntity.C2Entity>();
            await dbContext.DeleteAsync<RootEntity>();
            dbContext.Attach(new RootEntity(thisId: "1"));
            dbContext.Attach(new RootEntity.C1Entity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new RootEntity.C2Entity(thisId: "1.1.1", parentId: "1.1"));
            dbContext.Attach(new RootEntity.C3Entity(thisId: "1.1.1.1", parentId: "1.1.1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<RootEntity>()
                .BatchLoad(x => x.C1)
                    .ThenBatchLoad(x => x.C2)
                        .ThenBatchLoad(x => x.C3)
                .BatchLoad(x => x.FirstChild)
                    .ThenBatchLoad(x => x.C2)
                        .ThenBatchLoad(x => x.C3)
                .ToList();

            result.Count.ShouldBe(1);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.RootEntity" || x.ns == "mongodb-entities-test.C1Entity" || x.ns == "mongodb-entities-test.C2Entity" || x.ns == "mongodb-entities-test.C3Entity");
            logs.Count().ShouldBe(7);
            result.First(x => x.ThisId == "1").C1.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").C1.First().C2.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").C1.First().ThisId.ShouldBe("1.1");
            result.First(x => x.ThisId == "1").C1.First().C2.First().ThisId.ShouldBe("1.1.1");
            result.First(x => x.ThisId == "1").C1.First().C2.First().C3.First().ThisId.ShouldBe("1.1.1.1");
            logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.RootEntity" || x.ns == "mongodb-entities-test.C1Entity" || x.ns == "mongodb-entities-test.C2Entity" || x.ns == "mongodb-entities-test.C3Entity");
            logs.Count().ShouldBe(7);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task query_batch_load_should_work_with_nested_batch_load_for_single_item()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1.1", parentId: "1.1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<BatchLoadEntity>()
                .BatchLoad(x => x.Children)
                    .ThenBatchLoad(x => x.FirstChild)
                //.ThenBatchLoad(x => x.FirstChild)
                //.BatchLoad(x => x.FirstChild)
                //    .ThenBatchLoad(x => x.Children)
                .ToList();

            result.Count.ShouldBe(3);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(3);
            result.First(x => x.ThisId == "1").Children.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").Children.First().Children.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").Children.First().ThisId.ShouldBe("1.1");
            result.First(x => x.ThisId == "1").Children.First().FirstChild.Value.ThisId.ShouldBe("1.1.1");
            result.First(x => x.ThisId == "1").Children.First().Children.First().ThisId.ShouldBe("1.1.1");
            logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        [TestMethod]
        public async Task query_batch_load_should_work_with_nested_multiple_batch_load()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1.1", parentId: "1.1"));
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            await DB.RestartProfiler();
            var result = dbContext.Query<BatchLoadEntity>()
                .BatchLoad(x => x.Children)
                    .ThenBatchLoad(x => x.Children)
                    .BatchLoad(x => x.FirstChild)
                //.BatchLoad(x => x.FirstChild)
                //    .ThenBatchLoad(x => x.Children)
                .ToList();

            result.Count.ShouldBe(3);
            var logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(4);
            result.First(x => x.ThisId == "1").Children.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").Children.First().Children.Count().ShouldBe(1);
            result.First(x => x.ThisId == "1").Children.First().ThisId.ShouldBe("1.1");
            result.First(x => x.ThisId == "1").Children.First().FirstChild.Value.ThisId.ShouldBe("1.1.1");
            result.First(x => x.ThisId == "1").Children.First().Children.First().ThisId.ShouldBe("1.1.1");
            logs = DB.GetProfilerLogs().AsQueryable().Where(x => x.ns == "mongodb-entities-test.BatchLoadEntity");
            logs.Count().ShouldBe(4);
            DB.StopProfiler();
        }
    }
}
