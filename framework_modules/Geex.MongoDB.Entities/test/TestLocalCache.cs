using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Geex.Storage;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Entities.Tests.Models;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestLocalCache
    {
        [TestMethod]
        public async Task cache_should_not_exist_after_find()
        {
            var dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            var result = await dbContext.Find<TestEntity>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task cache_should_exist_after_queryable()
        {
            var dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            dbContext.Local[typeof(TestEntity)].ShouldNotBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task cache_should_not_exist_after_queryable_no_tracking()
        {
            var dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            var result = dbContext.Query<TestEntity>().AsNoTracking().FirstOrDefault();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            dbContext.Dispose();
        }


        [TestMethod]
        public async Task cache_not_modified_should_not_be_saved()
        {
            await DB.DeleteAsync<TestEntity>(x => true);
            var dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            dbContext.Local[typeof(TestEntity)].ShouldNotBeEmpty();
            var saveResult = await dbContext.SaveChanges();
            saveResult.Count.ShouldBe(0);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task multiple_query_should_share_instance_after_edit_except_find()
        {
            var dbContext = new DbContext();
            await dbContext.Query<TestEntity>().ToList().DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            var result1 = await dbContext.Find<TestEntity>().ExecuteFirstAsync();
            var result2 = dbContext.Query<TestEntity>().FirstOrDefault();
            result.GetHashCode().ShouldNotBe(result1.GetHashCode());
            result.GetHashCode().ShouldBe(result2.GetHashCode());
            result.GetHashCode().ShouldBe(dbContext.Local[typeof(TestEntity)].Values.FirstOrDefault().GetHashCode());
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task multiple_queryable_should_share_instance_after_edit()
        {
            var dbContext = new DbContext();
            await dbContext.Query<TestEntity>().ToList().DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.Name.ShouldBe("test");
            result.Name = "test1";
            result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.Name.ShouldBe("test1");

            dbContext.Dispose();
        }

        [TestMethod]
        public async Task newly_created_entity_should_be_in_cache()
        {
            var dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            dbContext.Local[typeof(TestEntity)].ShouldNotBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task delete_should_remove_cache()
        {
            var dbContext = new DbContext();
            //await dbContext.DeleteAsync<TestEntity>();
            //dbContext.Dispose();
            //dbContext = new DbContext();
            dbContext.OriginLocal[typeof(TestEntity)].Clear();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.OriginLocal[typeof(TestEntity)].ShouldBeEmpty();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.OriginLocal[typeof(TestEntity)].ShouldBeEmpty();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            dbContext.OriginLocal[typeof(TestEntity)].Count.ShouldBe(1);
            await result.DeleteAsync();
            dbContext.OriginLocal[typeof(TestEntity)].Count.ShouldBe(0);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task batch_delete_should_remove_cache()
        {
            var dbContext = new DbContext();
            await dbContext.Query<TestEntity>().ToList().DeleteAsync();
            var testEntities = new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "test"
                },
                new TestEntity()
                {
                    Name = "test1"
                }
            };
            //dbContext.Attach(testEntities);
            await testEntities.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().ToList();
            dbContext.Local[typeof(TestEntity)].Count.ShouldBeGreaterThan(0);
            await result.DeleteAsync();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task deleted_entity_should_be_filtered()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            dbContext.Local[typeof(TestEntity)].ShouldNotBeEmpty();
            dbContext.OriginLocal[typeof(TestEntity)].ShouldNotBeEmpty();
            await result.DeleteAsync();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            dbContext.OriginLocal[typeof(TestEntity)].ShouldBeEmpty();
            result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.ShouldBeNull();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task commit_time_save_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            var testEntity = new TestEntity()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.Name = "test1";
            result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.Name.ShouldBe("test1");
            result.Name = "test2";
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.Name.ShouldBe("test2");
            await result.DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<TestEntity>().FirstOrDefault();
            result.ShouldBeNull();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_add()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new TestEntity()
            {
                Name = "a1"
            };
            var list = new List<TestEntity>() { a1, };
            dbContext.Attach(list);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
            result.ToList().Count.ShouldBe(1);
            result.Count().ShouldBe(1);
            dbContext.Attach(new TestEntity()
            {
                Name = "a2"
            });
            var result1 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
            result1.ToList().Count.ShouldBe(2);
            result1.Count().ShouldBe(2);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Query<TestEntity>().Count(x => x.Name.StartsWith("a")).ShouldBe(2);
            dbContext.Dispose();
        }
        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_update()
        {
            var dbContext = new DbContext();
            {
                await dbContext.DeleteAsync<TestEntity>();
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var a1 = new TestEntity()
                {
                    Name = "a1"
                };
                var a2 = new TestEntity()
                {
                    Name = "a2"
                };
                var b1 = new TestEntity()
                {
                    Name = "b1"
                };
                var b2 = new TestEntity()
                {
                    Name = "b2"
                };
                var list = new List<TestEntity>() { a1, a2, b1, b2 };
                dbContext.Attach(list);
                await dbContext.SaveChanges();
                dbContext.Dispose();
            }
            {
                dbContext = new DbContext();
                var result = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
                result.ToList().Count().ShouldBe(2);
                result.Count().ShouldBe(2);
                var a1 = dbContext.Query<TestEntity>().First(x => x.Name == "a1");
                a1.Name = "1";
                var result1 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
                result1.ToList().Count().ShouldBe(1);
                result1.Count().ShouldBe(1);
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var result2 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
                result2.ToList().Count().ShouldBe(1);
                result2.Count().ShouldBe(1);
                dbContext.Dispose();
            }
        }

        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_update_with_complex_filter()
        {
            var dbContext = new DbContext();
            //prepare
            {
                await dbContext.DeleteAsync<TestEntity>();
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var a1 = new TestEntity()
                {
                    Name = "abc"
                };
                var a2 = new TestEntity()
                {
                    Name = "123"
                };
                var b1 = new TestEntity()
                {
                    Name = "abc123"
                };
                var b2 = new TestEntity()
                {
                    Name = "0"
                };
                var list = new List<TestEntity>() { a1, a2, b1, b2 };
                dbContext.Attach(list);
                await dbContext.SaveChanges();
                dbContext.Dispose();
            }
            {
                dbContext = new DbContext();
                var result = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("c"));
                result.ToList().Count().ShouldBe(1);
                result.Count().ShouldBe(1);
                var a1 = dbContext.Query<TestEntity>().First(x => x.Name == "abc");
                a1.Name = "1";
                var result1 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("c"));
                result1.ToList().Count().ShouldBe(0);
                result1.Count().ShouldBe(0);
                a1.Name = "a3";
                var result2 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("3"));
                result2.Count().ShouldBe(2);
                result2.ToList().Count().ShouldBe(2);
            }
        }
        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_delete()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new TestEntity()
            {
                Name = "a1"
            };
            var a2 = new TestEntity()
            {
                Name = "a2"
            };
            var b1 = new TestEntity()
            {
                Name = "b1"
            };
            var b2 = new TestEntity()
            {
                Name = "b2"
            };
            var list = new List<TestEntity>() { a1, a2, b1, b2 };
            dbContext.Attach(list);
            var result = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
            result.ToList().Count().ShouldBe(2);
            result.Count().ShouldBe(2);
            await a2.DeleteAsync();
            var result1 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
            result1.ToList().Count().ShouldBe(1);
            result1.Count().ShouldBe(1);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result2 = dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a"));
            result2.ToList().Count().ShouldBe(1);
            result2.Count().ShouldBe(1);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task cache_performance_test()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var data = Enumerable.Range(1, 9999).Select(x => new TestEntity()
            {
                Name = x.ToString()
            });

            dbContext.Attach(data);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var list = dbContext.Query<TestEntity>().ToList();
            dbContext.Dispose();
        }

        class ProtectedCtorClass : Entity<ProtectedCtorClass>
        {
            public string Name { get; set; }

            protected ProtectedCtorClass()
            {

            }

            public ProtectedCtorClass(string name)
            {
                Name = name;
            }
        }

        [TestMethod]
        public async Task protected_ctor_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<ProtectedCtorClass>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new ProtectedCtorClass("test");
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(TestEntity)].ShouldBeEmpty();
            var result = dbContext.Query<ProtectedCtorClass>().FirstOrDefault();
            dbContext.Local[typeof(ProtectedCtorClass)].ShouldNotBeEmpty();
            dbContext.Dispose();
        }
    }
}
