using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson.Serialization;
using MongoDB.Entities.Tests.Models;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestInheritance
    {
        public TestInheritance()
        {
        }
        [TestInitialize]
        public void Init()
        {
            BsonClassMap.LookupClassMap(typeof(InheritanceEntityChild)).Inherit<InheritanceEntity>();
        }
        [TestMethod]
        public async Task inheritance_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new InheritanceEntity()
            {
                Name = "a1"
            };
            var a2 = new InheritanceEntity()
            {
                Name = "a2"
            };
            var b1 = new InheritanceEntityChild()
            {
                Name = "b1"
            };
            var b2 = new InheritanceEntityChild()
            {
                Name = "b2"
            };
            var list = new List<InheritanceEntity>() { a1, a2, b1, b2 };
            dbContext.Attach(list);
            await dbContext.SaveChanges();


            dbContext = new DbContext();
             var result3 = dbContext.Query<IInheritanceEntity>().Where(x => x.Name.Contains("1"));
            result3.ToList().Count().ShouldBe(2);
            result3.Count().ShouldBe(2);
            var result = dbContext.Query<InheritanceEntity>().Where(x => x.Name.Contains("1"));
            result.ToList().Count().ShouldBe(2);
            result.Count().ShouldBe(2);
            var result1 = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().Where(x => x.Name.Contains("1"));
            result1.ToList().Count().ShouldBe(1);
            result1.Count().ShouldBe(1);
            var result2 = dbContext.Query<InheritanceEntityChild>().Where(x => x.Name.Contains("1"));
            result2.ToList().Count().ShouldBe(1);
            result2.Count().ShouldBe(1);
        }
        [TestMethod]
        public async Task cache_should_exist_after_queryable()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InheritanceEntity()
            {
                Name = "test"
            };
            var testEntityChild = new InheritanceEntityChild()
            {
                Name = "test1"
            };
            dbContext.Attach(testEntity);
            dbContext.Attach<InheritanceEntity>(testEntityChild);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Local[typeof(InheritanceEntity)].ShouldBeEmpty();
            var result = dbContext.Query<InheritanceEntity>().First(x => x.Id == testEntity.Id);
            dbContext.Local[typeof(InheritanceEntity)].Count.ShouldBe(1);
            var result1 = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().First(x => x.Id == testEntityChild.Id);
            dbContext.Local[typeof(InheritanceEntity)].Count.ShouldBe(2);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task multiple_query_should_share_instance_after_edit_except_find()
        {
            var dbContext = new DbContext();
            await dbContext.Query<InheritanceEntity>().ToList().DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            var result1 = await dbContext.Find<InheritanceEntity>().ExecuteFirstAsync();
            var result2 = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.GetHashCode().ShouldNotBe(result1.GetHashCode());
            result.GetHashCode().ShouldBe(result2.GetHashCode());
            result.GetHashCode().ShouldBe(dbContext.Local[typeof(InheritanceEntity)].Values.FirstOrDefault().GetHashCode());
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task multiple_queryable_should_share_instance_after_edit()
        {
            var dbContext = new DbContext();
            await dbContext.Query<InheritanceEntity>().ToList().DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.Name.ShouldBe("test");
            result.Name = "test1";
            result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.Name.ShouldBe("test1");

            dbContext.Dispose();
        }

        [TestMethod]
        public async Task newly_created_entity_should_be_in_cache()
        {
            var dbContext = new DbContext();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            dbContext.Local[typeof(InheritanceEntity)].ShouldNotBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task delete_should_remove_cache()
        {
            var dbContext = new DbContext();
            await dbContext.Query<InheritanceEntity>().ToList().DeleteAsync();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            dbContext.Local[typeof(InheritanceEntity)].ShouldNotBeEmpty();
            await result.DeleteAsync();
            dbContext.Local[typeof(InheritanceEntity)].ShouldBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task batch_delete_should_remove_cache()
        {
            var dbContext = new DbContext();
            await dbContext.Query<InheritanceEntity>().ToList().DeleteAsync();
            var testEntities = new List<InheritanceEntity>()
            {
                new InheritanceEntity()
                {
                    Name = "test"
                },
                new InheritanceEntityChild()
                {
                    Name = "test1"
                }
            };
            dbContext.Attach(testEntities);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().ToList().Cast<InheritanceEntity>().ToList();
            result.Count.ShouldBe(1);
            dbContext.Local[typeof(InheritanceEntity)].Count.ShouldBe(1);
            await result.DeleteAsync();
            dbContext.Local[typeof(InheritanceEntity)].ShouldBeEmpty();
            result = dbContext.Query<InheritanceEntity>().ToList();
            result.Count.ShouldBe(1);
            dbContext.Local[typeof(InheritanceEntity)].Count.ShouldBe(1);
            await result.DeleteAsync();
            dbContext.Local[typeof(InheritanceEntity)].ShouldBeEmpty();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task deleted_entity_should_be_filtered()
        {
            var dbContext = new DbContext();
            await dbContext.Query<InheritanceEntity>().ToList().DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            dbContext.Local[typeof(InheritanceEntity)].ShouldNotBeEmpty();
            dbContext.OriginLocal[typeof(InheritanceEntity)].ShouldNotBeEmpty();
            await result.DeleteAsync();
            dbContext.Local[typeof(InheritanceEntity)].ShouldBeEmpty();
            dbContext.OriginLocal[typeof(InheritanceEntity)].ShouldBeEmpty();
            result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.ShouldBeNull();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task commit_time_save_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach(testEntity);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().FirstOrDefault();
            result.Name = "test1";
            result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().FirstOrDefault();
            result.Name.ShouldBe("test1");
            result.Name = "test2";
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().FirstOrDefault();
            result.Name.ShouldBe("test2");
            await result.DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().FirstOrDefault();
            result.ShouldBeNull();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task commit_time_save_should_work1()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            var testEntity = new InheritanceEntityChild()
            {
                Name = "test"
            };
            dbContext.Attach<InheritanceEntity>(testEntity);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.Name = "test1";
            result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.Name.ShouldBe("test1");
            result.Name = "test2";
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().FirstOrDefault();
            result.Name.ShouldBe("test2");
            await result.DeleteAsync();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            result = dbContext.Query<InheritanceEntity>().FirstOrDefault();
            result.ShouldBeNull();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_add()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new InheritanceEntityChild()
            {
                Name = "a1"
            };
            var list = new List<InheritanceEntity>() { a1, };
            dbContext.Attach(list);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
            result.ToList().Count.ShouldBe(1);
            result.Count().ShouldBe(1);
            result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().Where(x => x.Name.StartsWith("a"));
            result.ToList().Count.ShouldBe(1);
            result.Count().ShouldBe(1);
            dbContext.Attach(new InheritanceEntity()
            {
                Name = "a2"
            });
            var result1 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
            result1.ToList().Count.ShouldBe(2);
            result1.Count().ShouldBe(2);
            var result2 = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().Where(x => x.Name.StartsWith("a"));
            result2.ToList().Count.ShouldBe(1);
            result2.Count().ShouldBe(1);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.Query<InheritanceEntity>().Count(x => x.Name.StartsWith("a")).ShouldBe(2);
            dbContext.Dispose();
        }
        internal interface INestedClass
        {
            public string Name { get; set; }
        }
        internal class NestedClass : EntityBase<NestedClass>, INestedClass
        {
            public NestedClass Inner { get; set; }
            public IQueryable<NestedClass> Inners => DbContext.Query<NestedClass>().Where(x => x.Name.StartsWith(this.Name));
            public IQueryable<string> AllNames => this.Inners.ToList().Select(x => x.Name).AsQueryable();
            public IQueryable<string> AllDisplayNames => this.Inners.ToList().Select(x => x.DisplayName).AsQueryable();

            public string Name { get; set; }
            public string DisplayName => Name + "1";
        }
        [TestMethod]
        public async Task nested_query_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<NestedClass>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var entity = new NestedClass()
            {
                Name = "1",
                Inner = new NestedClass()
                {
                    Name = "1.1"
                }
            };
            dbContext.Attach(entity);
            dbContext.Attach(new NestedClass()
            {
                Name = "1.1"
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var query = dbContext.Query<NestedClass>().Where(x => x.Name.StartsWith("1"));
            query.Count().ShouldBe(2);
            query.Count(x => x.Inner != default && x.Inner.Name.StartsWith("1.")).ShouldBe(1);
            var list = query.Where(x => x.Inner == null).ToList();
            list.Count.ShouldBe(1);
            list[0].AllNames.Count().ShouldBe(1);
            list[0].AllDisplayNames.Count().ShouldBe(1);
        }

        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_update()
        {
            var dbContext = new DbContext();
            {
                await dbContext.DeleteAsync<InheritanceEntity>();
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var a1 = new InheritanceEntity()
                {
                    Name = "a1"
                };
                var a2 = new InheritanceEntityChild()
                {
                    Name = "a2"
                };
                var b1 = new InheritanceEntity()
                {
                    Name = "b1"
                };
                var b2 = new InheritanceEntityChild()
                {
                    Name = "b2"
                };
                var list = new List<InheritanceEntity>() { a1, a2, b1, b2 };
                dbContext.Attach(list);
                await dbContext.SaveChanges();
                dbContext.Dispose();
            }
            {
                dbContext = new DbContext();
                var result = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
                result.ToList().Count().ShouldBe(2);
                result.Count().ShouldBe(2);
                result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().Where(x => x.Name.StartsWith("a"));
                result.ToList().Count().ShouldBe(1);
                result.Count().ShouldBe(1);
                var a2 = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().First(x => x.Name == "a2");
                a2.Name = "2";
                var result1 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
                result1.ToList().Count().ShouldBe(1);
                result1.Count().ShouldBe(1);
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var result2 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
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
                await dbContext.DeleteAsync<InheritanceEntity>();
                await dbContext.SaveChanges();
                dbContext.Dispose();
                dbContext = new DbContext();
                var a1 = new InheritanceEntity()
                {
                    Name = "abc"
                };
                var a2 = new InheritanceEntityChild()
                {
                    Name = "123"
                };
                var b1 = new InheritanceEntity()
                {
                    Name = "abc123"
                };
                var b2 = new InheritanceEntityChild()
                {
                    Name = "0"
                };
                var list = new List<InheritanceEntity>() { a1, a2, b1, b2 };
                dbContext.Attach(list);
                await dbContext.SaveChanges();
                dbContext.Dispose();
            }
            {
                dbContext = new DbContext();
                var result = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("c"));
                result.ToList().Count().ShouldBe(1);
                result.Count().ShouldBe(1);
                var a1 = dbContext.Query<InheritanceEntity>().First(x => x.Name == "abc");
                a1.Name = "1";
                var result1 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("c"));
                result1.ToList().Count().ShouldBe(0);
                result1.Count().ShouldBe(0);
                a1.Name = "a3";
                var result2 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a")).Where(x => x.Name.EndsWith("3"));
                result2.Count().ShouldBe(2);
                result2.ToList().Count().ShouldBe(2);
            }
        }
        [TestMethod]
        public async Task query_result_should_merge_local_cache_when_delete()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new InheritanceEntity()
            {
                Name = "a1"
            };
            var a2 = new InheritanceEntityChild()
            {
                Name = "a2"
            };
            var b1 = new InheritanceEntity()
            {
                Name = "b1"
            };
            var b2 = new InheritanceEntityChild()
            {
                Name = "b2"
            };
            var list = new List<InheritanceEntity>() { a1, a2, b1, b2 };
            dbContext.Attach(list);
            var result = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
            result.ToList().Count().ShouldBe(2);
            result.Count().ShouldBe(2);
            await a2.DeleteAsync();
            var result1 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
            result1.ToList().Count().ShouldBe(1);
            result1.Count().ShouldBe(1);
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var result2 = dbContext.Query<InheritanceEntity>().Where(x => x.Name.StartsWith("a"));
            result2.ToList().Count().ShouldBe(1);
            result2.Count().ShouldBe(1);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task both_subTypeQuery_and_ofType_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var a1 = new InheritanceEntity()
            {
                Name = "a1"
            };
            var a2 = new InheritanceEntityChild()
            {
                Name = "a2"
            };
            var b1 = new InheritanceEntity()
            {
                Name = "b1"
            };
            var b2 = new InheritanceEntityChild()
            {
                Name = "b2"
            };
            var list = new List<InheritanceEntity>() { a1, a2, b1, b2 };
            dbContext.Attach(list);
            //var result = dbContext.Queryable<InheritanceEntity>().Where(x => x.Name.StartsWith("a")).Select(x => x.Name);
            //result.ToList().Count().ShouldBe(2);
            //result.Count().ShouldBe(2);
            var result = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().Where(x => x.Name.StartsWith("a")).Select(x => x.Name);
            result.ToList().Count().ShouldBe(1);
            //result.Count().ShouldBe(1);
            //result = dbContext.Queryable<InheritanceEntityChild>().Where(x => x.Name.StartsWith("a")).Select(x => x.Name);
            //result.ToList().Count().ShouldBe(1);
            //result.Count().ShouldBe(1);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task cache_performance_test()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InheritanceEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var data = Enumerable.Range(1, 9999).Select(x =>
            {
                if (x % 2 == 1)
                {
                    return new InheritanceEntity()
                    {
                        Name = x.ToString()
                    };
                }
                return new InheritanceEntityChild()
                {
                    Name = x.ToString()
                };
            });
            var sw = new Stopwatch();
            sw.Start();
            dbContext.Attach(data);
            await dbContext.SaveChanges();
            sw.Stop();
            var t1 = sw.ElapsedMilliseconds;
            t1.ShouldBeLessThanOrEqualTo(2000);
            dbContext.Dispose();
            dbContext = new DbContext();
            sw.Restart();
            var list = dbContext.Query<InheritanceEntity>().ToList();
            list.Count().ShouldBe(9999);
            sw.Stop();
            var t2 = sw.ElapsedMilliseconds;
            t2.ShouldBeLessThanOrEqualTo(2000);
            sw.Restart();
            var list1 = dbContext.Query<InheritanceEntity>().OfType<InheritanceEntityChild>().ToList();
            list1.Count().ShouldBe(4999);
            sw.Stop();
            var t3 = sw.ElapsedMilliseconds;
            t3.ShouldBeLessThanOrEqualTo(2000);
            dbContext.Dispose();
        }

    }


}
