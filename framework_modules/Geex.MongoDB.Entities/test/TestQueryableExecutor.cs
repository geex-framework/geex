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
            //dbContext.DeleteAsync<TestEntity>().Wait();
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
            //dbContext.SaveChanges().Wait();
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
            dbContext.Query<TestEntity>().Where(x => x.Name.EndsWith("1")).Count().ShouldBe(3);
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
            await DB.DeleteAsync<TestEntity>();
            //dbContext = new DbContext();
            var data = Enumerable.Range(1, 200000).Select(x => new TestEntity()
            {
                Name = x.ToString()
            });
            var sw = new Stopwatch();
            sw.Start();
            await DB.Collection<TestEntity>().InsertManyAsync(data, new InsertManyOptions()
            {
                BypassDocumentValidation = true
            });
            //await dbContext.SaveChanges();
            sw.Stop();
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(60000);
            await Task.Delay(1000);
            var dbContext = new DbContext();
            sw.Restart();
            var list = dbContext.Query<TestEntity>().AsNoTracking().ToList();
            sw.Stop();
            list.Count().ShouldBe(200000);
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(3000);
            sw.Restart();
            var list1 = await dbContext.Find<TestEntity>().ExecuteAsync();
            sw.Stop();
            list1.Count().ShouldBe(200000);
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(1500);
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task performance_test()
        {
            var sw = new Stopwatch();
            sw.Start();
            var dbContext = new DbContext();
            var list = dbContext.Query<TestEntity>().AsNoTracking().ToList();
            sw.Stop();
            list.Count().ShouldBe(200000);
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(3000);
            sw.Stop();
            dbContext.Dispose();
        }

        [TestMethod]
        public async Task select_should_work()
        {

            var dbContext = new DbContext();
            dbContext = new DbContext();
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
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => x.Name).First().ShouldBe("a1");

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Select(x => x.Name).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => x.Name).ToList().Count().ShouldBe(5);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name }).First().Name.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { x.Value, x.Name }).ToList().Select(x => x.Name).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name }).First().SelectName.ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name }).ToList().Select(x => x.SelectName).First().ShouldBe("a1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("a1")).Select(x => new TestEntitySelectSubset(x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);

            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name }).First().Name.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { x.Value, x.Name }).ToList().Select(x => x.Name).First().ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name }).First().SelectName.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new { SelectValue = x.Value, SelectName = x.Name }).First().SelectName.ShouldBe("local1");
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Name, x.Value, x.Enum)).First().SelectEnum.ShouldBe(TestEntityEnum.Value1);
            dbContext.Query<TestEntity>().Where(x => x.Name.StartsWith("local1")).Select(x => new TestEntitySelectSubset(x.Name, x.Value, x.Enum)).ToList().Select(x => x.SelectEnum).First().ShouldBe(TestEntityEnum.Value1);

            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name }).Count().ShouldBe(5);
            dbContext.Query<TestEntity>().Select(x => new { x.Value, x.Name }).ToList().Count().ShouldBe(5);
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
                    DateTime = DateTime.Today,
                    DateTimeOffset = new DateTimeOffset(DateTime.Today)
                }
            });
            await dbContext.SaveChanges();
            dbContext.Dispose();

            dbContext = new DbContext();
            dbContext.Query<TestEntity>().Where(x => x.DateTime < DateTime.Now).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset < DateTime.Now).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTime == DateTime.Today).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset == new DateTimeOffset(DateTime.Today)).ToList().ShouldNotBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTime < DateTime.Now.AddDays(-1)).ToList().ShouldBeEmpty();
            dbContext.Query<TestEntity>().Where(x => x.DateTimeOffset < DateTime.Now.AddDays(-1)).ToList().ShouldBeEmpty();
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
    }
}
