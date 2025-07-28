using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Fixtures;
using MongoDB.Entities.Tests.Models;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestQueryableExecutor
    {
        public TestQueryableExecutor()
        {
            var dbContext = new DbContext();
            //prepare
            dbContext.DeleteAsync<TestEntity>().Wait();
            var a1 = new TestEntity()
            {
                Name = "a1",
                Enum = TestEntityEnum.Value1,
                Value = 1
            };
            var a2 = new TestEntity()
            {
                Name = "a2",
                Enum = TestEntityEnum.Value1,
                Value = 2
            };
            var b1 = new TestEntity()
            {
                Name = "b1",
                Enum = TestEntityEnum.Value1,
                Value = 3
            };
            var b2 = new TestEntity()
            {
                Name = "b2",
                Enum = TestEntityEnum.Value1,
                Value = 4
            };
            var list = new List<TestEntity>() { a1, a2, b1, b2 };
            dbContext.Attach(list);
            dbContext.SaveChanges().Wait();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task firstOrDefault_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            dbContext.Query<TestEntity>().Where(x => x.Name == "a1").FirstOrDefault().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().FirstOrDefault(x => x.Name == "a1").Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name == "a1").First().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().First(x => x.Name == "a1").Name.ShouldBe("a1");
            var firstOrDefault = dbContext.Query<TestEntity>().Where(x => x.Name == "a3").FirstOrDefault();
            firstOrDefault.ShouldBeNull();
            dbContext.Query<TestEntity>().FirstOrDefault(x => x.Name == "a3").ShouldBeNull();
            try
            {
                dbContext.Query<TestEntity>().Where(x => x.Name == "a3").First();
                throw new Exception("error");
            }
            catch (Exception e)
            {
                e.ShouldBeAssignableTo<InvalidOperationException>();
            }
            try
            {
                dbContext.Query<TestEntity>().First(x => x.Name == "a3");
                throw new Exception("error");
            }
            catch (Exception e)
            {
                e.ShouldBeAssignableTo<InvalidOperationException>();
            }
        }

        [TestMethod]
        public async Task singleOrDefault_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "a2"
                    }
                });
            dbContext.Query<TestEntity>().Where(x => x.Name == "a1").SingleOrDefault().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().SingleOrDefault(x => x.Name == "a1").Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name == "a1").Single().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Single(x => x.Name == "a1").Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name == "a3").SingleOrDefault().ShouldBeNull();
            dbContext.Query<TestEntity>().SingleOrDefault(x => x.Name == "a3").ShouldBeNull();
            try
            {
                dbContext.Query<TestEntity>().Where(x => x.Name == "a2").Single();
                throw new Exception("error");
            }
            catch (Exception e)
            {
                e.ShouldBeAssignableTo<InvalidOperationException>();
            }
            try
            {
                dbContext.Query<TestEntity>().Single(x => x.Name == "a2");
                throw new Exception("error");
            }
            catch (Exception e)
            {
                e.ShouldBeAssignableTo<InvalidOperationException>();
            }
        }

        [TestMethod]
        public async Task count_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1"
                    }
                });
            dbContext.Query<TestEntity>().Count(x => x.Name.EndsWith("1")).ShouldBe(3);
            dbContext.Query<TestEntity>().Where(x => x.Name.EndsWith("1")).ToList().Count.ShouldBe(3);
            dbContext.Query<TestEntity>().Count(x => x.Name.EndsWith("1")).ShouldBe(3);
            dbContext.Query<TestEntity>().Where(x => x.Name.EndsWith("1")).LongCount().ShouldBe(3);
            dbContext.Query<TestEntity>().LongCount(x => x.Name.EndsWith("1")).ShouldBe(3);
        }

        [TestMethod]
        public async Task distinct_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local5",
                        Value = 5
                    },
                    new TestEntity()
                    {
                        Name = "local5.1",
                        Value = 5
                    }
                });
            var count = dbContext.Query<TestEntity>().Select(x => x.Value).Distinct().Count();
            count.ShouldBe(5);
            count = dbContext.Query<TestEntity>().Select(x => x.Value).Distinct().ToList().Count;
            count.ShouldBe(5);
        }

        [TestMethod]
        public async Task any_should_work()
        {
            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1"
                    }
                });
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Any(x => x.Name.StartsWith("a1")).ShouldBe(true);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Any(x => x.Name.StartsWith("local1")).ShouldBe(true);
        }

        [TestMethod]
        public async Task query_performance_test()
        {
            var count = 200000;
            await DB.DeleteAsync<TestEntity>();
            //dbContext = new DbContext();
            var data = Enumerable.Range(1, count).Select(x => new TestEntity()
            {
                Name = x.ToString()
            });
            var sw = new Stopwatch();
            sw.Start();
            await DB.Collection<TestEntity>().InsertManyAsync(data, new InsertManyOptions()
            {
                BypassDocumentValidation = true,
            });
            //await dbContext.SaveChanges();
            sw.Stop();
            Debug.WriteLine("insert:" + sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(3000);
            await Task.Delay(1000);
            var dbContext = new DbContext();
            sw.Restart();
            var list = dbContext.Query<TestEntity>().AsNoTracking().ToList();
            sw.Stop();
            list.Count().ShouldBe(count);
            Debug.WriteLine("query:" + sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(3000);
            sw.Restart();
            var list1 = await dbContext.Find<TestEntity>().ExecuteAsync();
            sw.Stop();
            list1.Count().ShouldBe(count);
            Debug.WriteLine("find:" + sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(1500);
            dbContext.Dispose();
        }

        //[TestMethod]
        public async Task undefined_field_should_work()
        {
            var dbContext = new DbContext();
            var id = ObjectId.GenerateNewId().ToString();
            await dbContext.Collection<TestEntity>().InsertOneAsync(new TestEntity()
            {
                Id = id,
                Name = "local1",
                Value = 5,
                Enum = TestEntityEnum.Value1,
                Data = new[] { 1, 2 },
                DateTimeOffset = DateTimeOffset.Now
            });
            await dbContext.Collection<TestEntity>().UpdateOneAsync(x => x.Id == id, Builders<TestEntity>.Update.Unset(x => x.DateTimeOffset));
            var baseQuery = dbContext.Query<TestEntity>().Where(x => x.Id == id);
            baseQuery.Where(x => x.DateTimeOffset == default).ToList().Count.ShouldBe(1);
            baseQuery.Where(x => !x.DateTimeOffset.HasValue).ToList().Count.ShouldBe(1);
            baseQuery.Where(x => x.DateTimeOffset.HasValue).ToList().Count.ShouldBe(0);
        }

        [TestMethod]
        public async Task select_should_work()
        {

            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 5,
                        Enum = TestEntityEnum.Value1,
                        Data = new []{1,2}
                    }
                });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Select(x => x.Name).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => x.Name).ToList().Count().ShouldBe(5);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Select(x => x.Name).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => x.Name).ToList().Count().ShouldBe(5);


            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).First().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).First().Id.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).First().SelectName.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).First().SelectId.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).ToList().Select(x => x.SelectName).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).First().Name.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).First().Id.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).First().SelectName.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name, x.Id }).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name, x.Id }).ToList().Count().ShouldBe(5);
            //dbContext.Queryable<TestEntity>().SelectMany(x=>x.Data).Sum().ShouldBe(3);
            dbContext.Query<TestEntity>().Select(x => x.Data).ToList().SelectMany(x => (x ?? Array.Empty<int>()).ToList()).Sum().ShouldBe(3);
        }

        [TestMethod]
        public async Task select_should_not_work_with_calculated_property()
        {
            var dbContext = new DbContext();
            var entity = dbContext.Attach(new TestEntity()
            {
                Name = "local1",
                Value = 5,
                Enum = TestEntityEnum.Value1,
                Data = new[] { 1, 2 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext(entityTrackingEnabled: false);
            var testEntityQuery = dbContext.Query<TestEntity>().Where(x => x.Id == entity.Id);
            testEntityQuery.Select(x => x.Value).FirstOrDefault().ShouldBe(5);
            try
            {
                testEntityQuery.Select(x => x.ValuePlus1).FirstOrDefault().ShouldBe(6);
            }
            catch (Exception e)
            {
                e.Message.ShouldContain("ValuePlus1");
            }
            try
            {
                testEntityQuery.Select(x => x.ValuePlus1 + 1).FirstOrDefault().ShouldBe(7);
            }
            catch (Exception e)
            {
                e.Message.ShouldContain("ValuePlus1");
            }
        }

        [TestMethod]
        public async Task select_should_work_with_projection()
        {
            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 5,
                        Enum = TestEntityEnum.Value1,
                        Data = new []{1,2}
                    }
                });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext(entityTrackingEnabled: false);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset()
            {
                SelectId = x.Id,
                SelectName = x.Name,
                SelectValue = x.Value,
                SelectEnum = x.Enum,
                SelectDateTimeOffset = x.DateTimeOffset
            }).First().SelectName.ShouldBe("local1");

            try
            {
                dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x =>
                    new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)
                    {
                        SelectDateTimeOffset = x.DateTimeOffset
                    }).First().SelectName.ShouldBe("local1");
            }
            catch (Exception e)
            {
                e.Message.ShouldStartWith("Not supported express of: [new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)]");
            }

        }

        [TestMethod]
        public async Task select_should_work_with_no_tracking()
        {

            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 5,
                        Enum = TestEntityEnum.Value1,
                        Data = new []{1,2}
                    }
                });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext(entityTrackingEnabled: false);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Select(x => x.Name).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => x.Name).ToList().Count().ShouldBe(5);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Select(x => x.Name).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => x.Name).ToList().Count().ShouldBe(5);


            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).First().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).First().Id.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).First().SelectName.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).First().SelectId.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).ToList().Select(x => x.SelectName).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).First().Name.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).First().Id.ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name, x.Id }).ToList().Select(x => x.Id).First().ShouldNotBeNullOrEmpty();
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, x.Id }).First().SelectName.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name, SelectId = x.Id }).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).First().SelectId.ShouldNotBeNullOrEmpty();
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);
            //dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectId).First().ShouldNotBeNullOrEmpty();

            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name, x.Id }).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name, x.Id }).ToList().Count().ShouldBe(5);
            //dbContext.Queryable<TestEntity>().SelectMany(x=>x.Data).Sum().ShouldBe(3);
            dbContext.Query<TestEntity>().Select(x => x.Data).ToList().SelectMany(x => (x ?? Array.Empty<int>()).ToList()).Sum().ShouldBe(3);
        }

        [TestMethod]
        public async Task orderBy_should_work()
        {
            await DB.DeleteAsync<TestEntity>();
            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 1
                    },
                    new TestEntity()
                    {
                        Name = "local2",
                        Value = 2
                    },new TestEntity()
                    {
                        Name = "local3",
                        Value = 3
                    },new TestEntity()
                    {
                        Name = "local4",
                        Value = 4
                    },new TestEntity()
                    {
                        Name = "local5.1",
                        Value = 5
                    },new TestEntity()
                    {
                        Name = "local5",
                        Value = 5
                    },new TestEntity()
                    {
                        Name = "local5.2",
                        Value = 5
                    }
                });
            await dbContext.SaveChanges();
            await dbContext.SaveChanges();
            dbContext = new DbContext();
            var temp = dbContext.Query<TestEntity>().OrderBy(x => x.Value).ToList();
            temp.Select(x => x.Value).SequenceEqual(new[] { 1, 2, 3, 4, 5, 5, 5 }).ShouldBeTrue();
            var temp1 = dbContext.Query<TestEntity>().OrderBy(x => x.Value).Select(x => x.Value).ToList();
            temp1.SequenceEqual(new[] { 1, 2, 3, 4, 5, 5, 5 }).ShouldBeTrue();
            dbContext = new DbContext();
            temp1 = dbContext.Query<TestEntity>().OrderBy(x => x.Value).Select(x => x.Value).ToList();
            temp1.SequenceEqual(new[] { 1, 2, 3, 4, 5, 5, 5 }).ShouldBeTrue();

            var temp2 = dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Name).ToList();
            temp2.SequenceEqual(new[] { "local1", "local2", "local3", "local4", "local5", "local5.1", "local5.2" }).ShouldBeTrue();
            dbContext = new DbContext();
            temp2 = dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Name).ToList();
            temp2.SequenceEqual(new[] { "local1", "local2", "local3", "local4", "local5", "local5.1", "local5.2" }).ShouldBeTrue();

            dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Value).Sum().ShouldBe(25);
            dbContext = new DbContext();
            dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Value).Sum().ShouldBe(25);
            dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Value).Count().ShouldBe(7);
            dbContext = new DbContext();
            dbContext.Query<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Value).Count().ShouldBe(7);

            //var temp3 = dbContext.Queryable<TestEntity>().OrderBy(x => x.Value).ThenBy(x => x.Name).Select(x => x.Name).Skip(4).ToList();
            //temp3.SequenceEqual(new[] { "local5", "local5.1", "local5.2" }).ShouldBeTrue();
        }

        [TestMethod]
        public async Task sum_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 5
                    }
                });
            dbContext.Query<TestEntity>().Sum(x => x.Value).ShouldBe(1 + 2 + 3 + 4 + 5);
            dbContext.Query<TestEntity>().Select(x => x.Value).Sum().ShouldBe(1 + 2 + 3 + 4 + 5);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a")).Sum(x => x.Value).ShouldBe(1 + 2);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a")).Select(x => x.Value).Sum().ShouldBe(1 + 2);
        }

        [TestMethod]
        public async Task filter_by_date_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "local1",
                    DateTime = DateTime.Today.ToUniversalTime(),
                    DateTimeOffset = new DateTimeOffset(DateTime.Today.ToUniversalTime())
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();
            dbContext.Query<TestEntity>().Where(x => x.DateTime < DateTime.UtcNow).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset < DateTime.UtcNow).ToList().ShouldNotBeEmpty();
            DB.StartProfiler();
            dbContext.Query<TestEntity>().Where(x => x.DateTime == DateTime.Today.ToUniversalTime()).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset == new DateTimeOffset(DateTime.Today.ToUniversalTime())).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTime < DateTime.UtcNow.AddDays(-1)).ToList().ShouldBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset < DateTime.UtcNow.AddDays(-1)).ToList().ShouldBeEmpty();
        }

        [TestMethod]
        public async Task skip_and_take_should_work()
        {
            await DB.DeleteAsync<TestEntity>();
            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                    new TestEntity(),
                });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var PageIndex = 2;
            var PageSize = 12;
            var result = dbContext.Query<TestEntity>();
            var items = result.Skip((PageIndex - 1) * PageSize).Skip(1).Take(PageSize).ToList();
            //var pageList = new PagedList<TestEntity>(result, PageIndex, PageSize);
            items[0].As<IEntityBase>().DbContext.ShouldNotBeNull();
            items.Count.ShouldBe(8);
        }

        [TestMethod]
        public async Task skip_and_take_should_work_when_combine_with_sort()
        {
            await DB.DeleteAsync<TestEntity>();
            var dbContext = new DbContext();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity(){Value = 0},
                    new TestEntity(){Value = 1},
                    new TestEntity(){Value = 2},
                    new TestEntity(){Value = 3},
                    new TestEntity(){Value = 4},
                    new TestEntity(){Value = 5},
                    new TestEntity(){Value = 6},
                    new TestEntity(){Value = 7},
                    new TestEntity(){Value = 8},
                    new TestEntity(){Value = 9},
                    new TestEntity(){Value = 10},
                    new TestEntity(){Value = 11},
                    new TestEntity(){Value = 12},
                    new TestEntity(){Value = 13},
                    new TestEntity(){Value = 14},
                    new TestEntity(){Value = 15},
                    new TestEntity(){Value = 16},
                    new TestEntity(){Value = 17},
                    new TestEntity(){Value = 18},
                    new TestEntity(){Value = 19},
                    new TestEntity(){Value = 20},
                });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var PageIndex = 2;
            var PageSize = 12;
            var result = dbContext.Query<TestEntity>().Where(x => x.Value < 18);
            result = result.OrderByDescending(x => x.Value);
            var items = result.Skip((PageIndex - 1) * PageSize).Skip(1).Take(PageSize).ToList();
            //var pageList = new PagedList<TestEntity>(result, PageIndex, PageSize);
            items[0].As<IEntityBase>().DbContext.ShouldNotBeNull();
            items.Count.ShouldBe(5);
            var values = items.Select(x => x.Value).ToList();
            values.SequenceEqual(new[] { 4, 3, 2, 1, 0 }).ShouldBeTrue();
        }

        [TestMethod]
        public async Task interfaced_entity_should_work()
        {
            var dbContext = new DbContext();
            dbContext = new DbContext();
            IQueryable<ITestEntity> interfaces = dbContext.Query<TestEntity>();
            dbContext.Attach(new List<TestEntity>()
                {
                    new TestEntity()
                    {
                        Name = "local1",
                        Value = 5
                    }
                });
            interfaces.Sum(x => x.Value).ShouldBe(1 + 2 + 3 + 4 + 5);
            interfaces.Select(x => x.Value).Sum().ShouldBe(1 + 2 + 3 + 4 + 5);
            interfaces.Where(x => x.Name.StartsWith("a")).Sum(x => x.Value).ShouldBe(1 + 2);
            interfaces.Where(x => x.Name.StartsWith("a")).Any().ShouldBe(true);
            interfaces.Where(x => x.Name.StartsWith("a")).Select(x => x.Value).Any().ShouldBe(true);
            interfaces.Where(x => x.Name.StartsWith("a")).ToList().Count.ShouldBe(2);
        }

        [TestMethod]

        public async Task contains_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            var attached = dbContext.Attach(new TestEntity()
            {
                Name = "local1",
                Value = 5
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var id = attached.Id;
            var array = new[] { "local1", "local_not_exist" };
            var list = dbContext.Query<TestEntity>().Where(x => array.Contains(x.Name)).ToList();
            list.First().Id.ShouldBe(id);
            dbContext.Query<TestEntity>().Where(x => array.Contains(x.Name)).Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Any(x => array.Contains(x.Name)).ShouldBe(true);
        }

        [TestMethod]
        public async Task contains_of_object_id_cast_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            var attached = dbContext.Attach(new TestEntity()
            {
                Name = "local1",
                Value = 5,
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var id = attached.Id;
            var idList = new[] { ObjectId.GenerateNewId().ToString(), id }.ToList();
            dbContext.Query<TestEntity>().Where(x => idList.Contains(x.Id)).ToList().ShouldNotBeEmpty();
            dbContext.Dispose();
            dbContext = new DbContext();
            var array = new[] { ObjectId.GenerateNewId().ToString(), id };
            dbContext.Query<TestEntity>().Where(x => array.Contains(x.Id)).ToList().ShouldNotBeEmpty();
            var list = dbContext.Query<TestEntity>().Where(x => array.Contains(x.Id)).ToList();
            list.First().Id.ShouldBe(id);
            dbContext.Query<TestEntity>().Where(x => array.Contains(x.Id)).Any().ShouldBe(true);
            dbContext.Query<TestEntity>().Any(x => array.Contains(x.Id)).ShouldBe(true);
        }

        [TestMethod]
        public async Task array_intersection_query_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();
            dbContext.Attach(new TestEntity()
            {
                Name = "local1",
                Value = 5,
                Data = new[] { 1, 2, 3 }
            });
            dbContext.Attach(new TestEntity()
            {
                Name = "local2",
                Value = 5,
                Data = new[] { 1, 2 }
            });
            dbContext.Attach(new TestEntity()
            {
                Name = "local2",
                Value = 5,
                Data = new[] { 3 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var array = new List<int>() { 2, 3 };
            var result = dbContext.Query<TestEntity>().Where(x => array.Intersect(x.Data).Any()).ToList();
            result.Count.ShouldBe(3);
        }

        [TestMethod]
        public async Task where_condition_should_be_optimized()
        {
            var dbContext = await GetDbContext();
        }

        private static async Task<DbContext> GetDbContext()
        {
            var dbContext = new DbContext(TestFixture.ServiceProvider);
            await dbContext.DeleteAsync<TestEntity>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            return dbContext;
        }

        [TestMethod]
        public async Task math_functions_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "math1", Value = -5 },
                new TestEntity() { Name = "math2", Value = 9 },
                new TestEntity() { Name = "math3", Value = 16 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试绝对值
            var absResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "math1")
                .Select(x => Math.Abs(x.Value))
                .First();
            absResult.ShouldBe(5);

            // 测试平方根
            var sqrtResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "math2")
                .Select(x => Math.Sqrt(x.Value))
                .First();
            sqrtResult.ShouldBe(3.0, 0.001);

            // 测试幂运算
            var powResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "math2")
                .Select(x => Math.Pow(x.Value, 2))
                .First();
            powResult.ShouldBe(81.0, 0.001);

            // 测试向上取整
            var ceilResult = dbContext.Query<TestEntity>()
                .Select(x => Math.Ceiling(x.Value + 0.3))
                .ToList();
            ceilResult.ShouldNotBeEmpty();

            // 测试向下取整
            var floorResult = dbContext.Query<TestEntity>()
                .Select(x => Math.Floor(x.Value + 0.7))
                .ToList();
            floorResult.ShouldNotBeEmpty();

            // 测试四舍五入
            var roundResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "math1")
                .Select(x => Math.Round(x.Value * 1.567, 1))
                .First();
            roundResult.ShouldNotBe(0);
        }

        [TestMethod]
        public async Task string_operations_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "  hello world  ", Value = 1 },
                new TestEntity() { Name = "UPPER case", Value = 2 },
                new TestEntity() { Name = "lower CASE", Value = 3 },
                new TestEntity() { Name = "replace_this_text", Value = 4 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试Trim
            var trimResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 1)
                .Select(x => x.Name.Trim())
                .First();
            trimResult.ShouldBe("hello world");

            // 测试ToUpper
            var upperResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 2)
                .Select(x => x.Name.ToUpper())
                .First();
            upperResult.ShouldBe("UPPER CASE");

            // 测试ToLower
            var lowerResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 3)
                .Select(x => x.Name.ToLower())
                .First();
            lowerResult.ShouldBe("lower case");

            // 测试Replace
            var replaceResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 4)
                .Select(x => x.Name.Replace("_", " "))
                .First();
            replaceResult.ShouldBe("replace this text");

            // 测试字符串长度
            var lengthResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 1)
                .Select(x => x.Name.Trim().Length)
                .First();
            lengthResult.ShouldBe(11); // "hello world".Length

            // 测试Substring
            var substringResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value == 1)
                .Select(x => x.Name.Trim().Substring(0, 5))
                .First();
            substringResult.ShouldBe("hello");
        }

        [TestMethod]
        public async Task array_operations_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "array1",
                    Value = 1,
                    Data = new[] { 1, 2, 3, 4, 5 }
                },
                new TestEntity()
                {
                    Name = "array2",
                    Value = 2,
                    Data = new[] { 10, 20, 30 }
                },
                new TestEntity()
                {
                    Name = "array3",
                    Value = 3,
                    Data = new[] { 100, 200 }
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试数组长度
            var arrayLengthResult = dbContext
                .Query<TestEntity>()
                .Where(x => x.Name == "array1")
                .Count(x => x.Data.Length == 5);
            arrayLengthResult.ShouldBe(1);

            // 测试数组包含
            var containsResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data.Contains(3));
            containsResult.ShouldBe(1);

            // 测试数组Any
            var anyResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data.Any(d => d > 25));
            anyResult.ShouldBe(2); // array2 和 array3
        }

        [TestMethod]
        public async Task date_operations_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            var testDate = new DateTime(2023, 6, 15, 14, 30, 45);
            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "date1",
                    Value = 1,
                    DateTime = testDate
                },
                new TestEntity()
                {
                    Name = "date2",
                    Value = 2,
                    DateTime = testDate.AddDays(10)
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试年份提取
            var yearResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "date1")
                .Select(x => x.DateTime.Year)
                .First();
            yearResult.ShouldBe(2023);

            // 测试月份提取
            var monthResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "date1")
                .Select(x => x.DateTime.Month)
                .First();
            monthResult.ShouldBe(6);

            // 测试日期提取
            var dayResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "date1")
                .Select(x => x.DateTime.Day)
                .First();
            dayResult.ShouldBe(15);

            // 测试小时提取
            var hourResult = dbContext.Query<TestEntity>()
                .Where(x => x.Name == "date1")
                .Select(x => x.DateTime.Hour)
                .First();
            hourResult.ShouldBe(14);

            // 测试日期范围查询
            var rangeResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime >= testDate && x.DateTime < testDate.AddDays(5));
            rangeResult.ShouldBe(1);
        }

        [TestMethod]
        public async Task complex_expressions_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "complex1",
                    Value = 10,
                    Data = new[] { 1, 2, 3 }
                },
                new TestEntity()
                {
                    Name = "complex2",
                    Value = 20,
                    Data = new[] { 4, 5, 6 }
                },
                new TestEntity()
                {
                    Name = "complex3",
                    Value = 30,
                    Data = new[] { 7, 8, 9 }
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试复合表达式：数学运算 + 字符串操作
            var complexResult = dbContext.Query<TestEntity>()
                .Where(x => x.Value > 15)
                .Select(x => new
                {
                    ProcessedName = x.Name.ToUpper().Replace("COMPLEX", "PROC"),
                    CalculatedValue = Math.Pow(x.Value, 2),
                    ArraySum = x.Data.Sum(),
                    ArrayAvg = x.Data.Average(),
                    ArrayCount = x.Data.Length
                })
                .OrderBy(x=>x.ProcessedName)
                .ToList();

            complexResult.ShouldNotBeEmpty();
            complexResult.Count.ShouldBe(2);

            var firstResult = complexResult.First();
            firstResult.ProcessedName.ShouldBe("PROC2");
            firstResult.CalculatedValue.ShouldBe(400.0, 0.001);
            firstResult.ArraySum.ShouldBe(15);
            firstResult.ArrayCount.ShouldBe(3);
        }

        [TestMethod]
        public async Task conditional_expressions_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "cond1", Value = 5 },
                new TestEntity() { Name = "cond2", Value = 15 },
                new TestEntity() { Name = "cond3", Value = 25 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试条件表达式
            var conditionalResult = dbContext.Query<TestEntity>()
                .Select(x => new
                {
                    Name = x.Name,
                    Category = x.Value > 20 ? "High" : (x.Value > 10 ? "Medium" : "Low"),
                    IsEven = x.Value % 2 == 0,
                    AbsoluteValue = Math.Abs(x.Value - 15)
                })
                .OrderBy(x => x.Name)
                .ToList();

            conditionalResult.ShouldNotBeEmpty();
            conditionalResult.Count.ShouldBe(3);

            conditionalResult[0].Category.ShouldBe("Low");
            conditionalResult[1].Category.ShouldBe("Medium");
            conditionalResult[2].Category.ShouldBe("High");
        }

        [TestMethod]
        public async Task string_search_operations_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "test@example.com", Value = 1 },
                new TestEntity() { Name = "user@domain.org", Value = 2 },
                new TestEntity() { Name = "invalid-email", Value = 3 },
                new TestEntity() { Name = "another@test.net", Value = 4 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试StartsWith
            var startsWithResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.StartsWith("test"));
            startsWithResult.ShouldBe(1);

            // 测试EndsWith
            var endsWithResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.EndsWith(".com"));
            endsWithResult.ShouldBe(1);

            // 测试Contains
            var containsResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.Contains("@"));
            containsResult.ShouldBe(3);

            // 测试IndexOf
            var indexOfResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.IndexOf("@") > 0);
            indexOfResult.ShouldBe(3);
        }

        [TestMethod]
        public async Task null_handling_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "valid", Value = 1 },
                new TestEntity() { Name = null, Value = 2 },
                new TestEntity() { Name = "", Value = 3 },
                new TestEntity() { Name = "   ", Value = 4 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试IsNullOrEmpty
            var nullOrEmptyResult = dbContext
                .Query<TestEntity>()
                .Count(x => string.IsNullOrEmpty(x.Name));
            nullOrEmptyResult.ShouldBe(2); // null和空字符串

            // 测试null合并
            var coalescingResult = dbContext.Query<TestEntity>()
                .Select(x => new
                {
                    SafeName = x.Name ?? "default",
                    TrimmedName = (x.Name ?? "").Trim()
                })
                .ToList();

            coalescingResult.ShouldNotBeEmpty();
            coalescingResult.Count.ShouldBe(4);

            var nullRecord = coalescingResult.First(x => x.SafeName == "default");
            nullRecord.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task where_math_expressions_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "math1", Value = -10 },
                new TestEntity() { Name = "math2", Value = 25 },
                new TestEntity() { Name = "math3", Value = 16 },
                new TestEntity() { Name = "math4", Value = 100 },
                new TestEntity() { Name = "math5", Value = 0 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试绝对值筛选
            var absFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Abs(x.Value) > 15);
            absFilterResult.ShouldBe(3); // -10(abs=10不符合), 25, 16不符合, 100

            // 测试平方根筛选
            var sqrtFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Value > 0 && Math.Sqrt(x.Value) == 5);
            sqrtFilterResult.ShouldBe(1); // 25的平方根是5

            // 测试幂运算筛选
            var powFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Pow(x.Value, 2) > 400);
            powFilterResult.ShouldBe(2); // 25^2=625, 100^2=10000

            // 测试取整筛选
            var ceilFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Ceiling(x.Value / 10.0) == 3);
            ceilFilterResult.ShouldBe(1); // 25/10=2.5, ceiling=3

            // 测试符号筛选
            var signFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Sign(x.Value) == -1);
            signFilterResult.ShouldBe(1); // 只有-10是负数

            // 测试最值筛选
            var minMaxFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Min(x.Value, 50) == x.Value);
            minMaxFilterResult.ShouldBe(4); // 除了100，其他都小于等于50
        }

        [TestMethod]
        public async Task where_string_pattern_matching_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "user@example.com", Value = 1 },
                new TestEntity() { Name = "admin@test.org", Value = 2 },
                new TestEntity() { Name = "  spaced text  ", Value = 3 },
                new TestEntity() { Name = "MixedCase123", Value = 4 },
                new TestEntity() { Name = "special-chars_here", Value = 5 },
                new TestEntity() { Name = "verylongstringwithoutspaces", Value = 6 },
                new TestEntity() { Name = "", Value = 7 },
                new TestEntity() { Name = null, Value = 8 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试长度筛选
            var lengthFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && x.Name.Length > 15);
            lengthFilterResult.ShouldBe(3); // "user@example.com", "special-chars_here", "verylongstringwithoutspaces"

            // 测试Trim后长度筛选
            var trimLengthResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && x.Name.Trim().Length < 5);
            trimLengthResult.ShouldBe(1); // 空字符串trim后长度为0

            // 测试包含特殊字符
            var specialCharsResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && (x.Name.Contains("@") || x.Name.Contains("_")));
            specialCharsResult.ShouldBe(3); // 包含@或_的

            // 测试大小写组合筛选
            var caseFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && x.Name.ToLower().Contains("test"));
            caseFilterResult.ShouldBe(1); // "admin@test.org"

            // 测试字符串替换后筛选
            var replaceFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && x.Name.Replace("-", "").Replace("_", "").Length > 16);
            replaceFilterResult.ShouldBe(1); // 替换特殊字符后长度大于16的

            // 测试IndexOf位置筛选
            var indexFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name != null && x.Name.IndexOf("@") > 0 && x.Name.IndexOf("@") < 10);
            indexFilterResult.ShouldBe(2); // @符号在1-9位置的
        }

        [TestMethod]
        public async Task where_complex_boolean_logic_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity() { Name = "test1", Value = 10, Enum = TestEntityEnum.Value1 },
                new TestEntity() { Name = "test2", Value = 20, Enum = TestEntityEnum.Value1 },
                new TestEntity() { Name = "prod1", Value = 30, Enum = TestEntityEnum.Value2 },
                new TestEntity() { Name = "prod2", Value = 40, Enum = TestEntityEnum.Value2 },
                new TestEntity() { Name = "dev1", Value = 50, Enum = TestEntityEnum.Value1 },
                new TestEntity() { Name = "dev2", Value = 60, Enum = TestEntityEnum.Value2 }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试复杂AND条件
            var complexAndResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.StartsWith("test") && x.Value > 15 && x.Enum == TestEntityEnum.Value1);
            complexAndResult.ShouldBe(1); // 只有test2符合

            // 测试复杂OR条件
            var complexOrResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.StartsWith("dev") || (x.Value > 35 && x.Enum == TestEntityEnum.Value2));
            complexOrResult.ShouldBe(3); // dev1, dev2, prod2

            // 测试嵌套条件
            var nestedConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => (x.Name.StartsWith("test") || x.Name.StartsWith("prod")) &&
                           (x.Value % 20 == 0) &&
                           x.Enum == TestEntityEnum.Value1);
            nestedConditionResult.ShouldBe(1); // test2 (Value=20, 能被20整除, Enum=Value1)

            // 测试NOT条件
            var notConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => !x.Name.Contains("1") && x.Value > 25);
            notConditionResult.ShouldBe(2); // prod2, dev2 (不包含"1"且Value>25)

            // 测试范围条件
            var rangeConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Value >= 20 && x.Value <= 50 && x.Name.Length == 5);
            rangeConditionResult.ShouldBe(3); // test2(20), prod1(30), prod2(40)
        }

        [TestMethod]
        public async Task where_array_filtering_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "array1",
                    Value = 1,
                    Data = new[] { 1, 2, 3, 4, 5 }
                },
                new TestEntity()
                {
                    Name = "array2",
                    Value = 2,
                    Data = new[] { 10, 20, 30, 40 }
                },
                new TestEntity()
                {
                    Name = "array3",
                    Value = 3,
                    Data = new[] { 100, 200 }
                },
                new TestEntity()
                {
                    Name = "array4",
                    Value = 4,
                    Data = new[] { 5, 15, 25, 35, 45 }
                },
                new TestEntity()
                {
                    Name = "empty",
                    Value = 5,
                    Data = new int[0]
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试数组长度筛选
            var lengthFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Length > 3);
            lengthFilterResult.ShouldBe(3); // array1(5), array2(4), array4(5)

            // 测试数组包含特定值
            var containsSpecificResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Contains(5));
            containsSpecificResult.ShouldBe(2); // array1, array4

            // 测试数组Any条件
            var anyConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Any(d => d > 50));
            anyConditionResult.ShouldBe(1); // 只有array3有大于50的值

            // 测试数组All条件
            var allConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Length > 0 && x.Data.All(d => d > 0));
            allConditionResult.ShouldBe(4); // 除了empty，所有数组的元素都大于0

            // 测试数组范围条件
            var rangeConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Any(d => d >= 10 && d <= 30));
            rangeConditionResult.ShouldBe(2); // array2, array4都有10-30范围内的值

            // 测试空数组
            var emptyArrayResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Data != null && x.Data.Length == 0);
            emptyArrayResult.ShouldBe(1); // 只有empty数组
        }

        [TestMethod]
        public async Task where_date_filtering_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            var baseDate = new DateTime(2023, 6, 15, 14, 30, 45);
            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "date1",
                    Value = 1,
                    DateTime = baseDate // 2023-06-15 14:30:45
                },
                new TestEntity()
                {
                    Name = "date2",
                    Value = 2,
                    DateTime = baseDate.AddMonths(2) // 2023-08-15 14:30:45
                },
                new TestEntity()
                {
                    Name = "date3",
                    Value = 3,
                    DateTime = baseDate.AddYears(1) // 2024-06-15 14:30:45
                },
                new TestEntity()
                {
                    Name = "date4",
                    Value = 4,
                    DateTime = new DateTime(2023, 12, 25, 10, 0, 0) // 圣诞节
                },
                new TestEntity()
                {
                    Name = "date5",
                    Value = 5,
                    DateTime = new DateTime(2023, 1, 1, 0, 0, 0) // 新年
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试年份筛选
            var yearFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.Year == 2023);
            yearFilterResult.ShouldBe(4); // 除了date3，都是2023年

            // 测试月份筛选
            var monthFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.Month == 6);
            monthFilterResult.ShouldBe(2); // date1和date3都是6月

            // 测试季度筛选（使用月份）
            var quarterFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.Month >= 7 && x.DateTime.Month <= 9);
            quarterFilterResult.ShouldBe(1); // 只有date2是第三季度(8月)

            // 测试小时筛选
            var hourFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.Hour >= 10 && x.DateTime.Hour < 15);
            hourFilterResult.ShouldBe(4); // date1(14), date2(14), date3(14), date4(10)

            // 测试工作日筛选（周一到周五）
            var weekdayFilterResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.DayOfWeek != DayOfWeek.Saturday && x.DateTime.DayOfWeek != DayOfWeek.Sunday);
            weekdayFilterResult.ShouldBeGreaterThan(0);

            // 测试日期范围筛选
            var dateRangeResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime >= new DateTime(2023, 6, 1) && x.DateTime < new DateTime(2023, 9, 1, 0, 0, 0));
            dateRangeResult.ShouldBe(2); // date1(6月), date2(8月)

            // 测试今年筛选
            var thisYearResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime.Year == 2023);
            thisYearResult.ShouldBe(4);
        }

        [TestMethod]
        public async Task where_combined_conditions_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TestEntity>();

            dbContext.Attach(new List<TestEntity>()
            {
                new TestEntity()
                {
                    Name = "active_user_1",
                    Value = 25,
                    Enum = TestEntityEnum.Value1,
                    DateTime = DateTime.Now.AddDays(-10),
                    Data = new[] { 1, 2, 3 }
                },
                new TestEntity()
                {
                    Name = "inactive_user_2",
                    Value = 15,
                    Enum = TestEntityEnum.Value2,
                    DateTime = DateTime.Now.AddDays(-100),
                    Data = new[] { 10, 20 }
                },
                new TestEntity()
                {
                    Name = "active_admin_3",
                    Value = 15,
                    Enum = TestEntityEnum.Value1,
                    DateTime = DateTime.Now.AddDays(-5),
                    Data = new[] { 100, 200, 300, 400 }
                },
                new TestEntity()
                {
                    Name = "test_account",
                    Value = 5,
                    Enum = TestEntityEnum.Value2,
                    DateTime = DateTime.Now.AddDays(-1),
                    Data = new int[0]
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();

            // 测试复合业务逻辑筛选：活跃用户
            var activeUsersResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Name.Contains("active") &&
                           x.Value > 20 &&
                           x.DateTime > DateTime.Now.AddDays(-30) &&
                           x.Data.Length > 2);
            activeUsersResult.ShouldBe(1); // active_user_1

            // 测试数学表达式与字符串组合
            var mathStringComboResult = dbContext
                .Query<TestEntity>()
                .Count(x => Math.Abs(x.Value - 20) <= 10 &&
                           x.Name.ToLower().Contains("user") &&
                           x.Name.Replace("_", " ").Length > 10);
            mathStringComboResult.ShouldBe(2); // active_user_1, inactive_user_2

            // 测试数组与枚举组合
            var arrayEnumComboResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.Enum == TestEntityEnum.Value1 &&
                           x.Data.Any() &&
                           x.Data.All(d => d < 500));
            arrayEnumComboResult.ShouldBe(2); // active_user_1, active_admin_3

            // 测试日期与数值范围组合
            var dateValueComboResult = dbContext
                .Query<TestEntity>()
                .Count(x => x.DateTime > DateTime.Now.AddDays(-50) &&
                           x.Value >= 10 && x.Value <= 30 &&
                           x.Name.Length > 5);
            dateValueComboResult.ShouldBe(2); // active_user_1, active_admin_3

            // 测试三元条件在Where中的使用
            var ternaryConditionResult = dbContext
                .Query<TestEntity>()
                .Count(x => (x.Enum == TestEntityEnum.Value1 ? x.Value > 20 : x.Value < 20) &&
                           x.Name != null);
            ternaryConditionResult.ShouldBe(3); // active_user_1(Value1,25>20), inactive_user_2(Value2,15<20), active_admin_4(Value1,5<20)
        }
    }
}
