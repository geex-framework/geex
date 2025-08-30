using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestQueryPerformance
    {
        [TestMethod]
        public async Task query_performance_test()
        {
            await SetupTestData();
            await TestBasicQueries();
            await TestComplexQueries();
            await TestAggregationQueries();
            await TestProjectionQueries();
            await TestPaginationQueries();
        }

        private async Task SetupTestData()
        {
            var nameIndex = new IndexKeysDefinitionBuilder<TestEntity>().Text(x => x.Name);
            var idIndex = new IndexKeysDefinitionBuilder<TestEntity>().Hashed(x => x.Id);
            var createIndexOptions = new CreateIndexOptions<TestEntity>();
            var nameIndexModel = new CreateIndexModel<TestEntity>(nameIndex, createIndexOptions);
            var idIndexModel = new CreateIndexModel<TestEntity>(idIndex, createIndexOptions);
            await Cache<TestEntity>.Collection.Indexes.CreateOneAsync(idIndexModel);
            await Cache<TestEntity>.Collection.Indexes.CreateOneAsync(nameIndexModel);
            var count = 200000;
            Debug.WriteLine("=== Setting up test data ===");

            await DB.DeleteAsync<TestEntity>();
            var baseDateTime = DateTime.UtcNow.AddDays(-365);
            var random = new Random(42); // 固定种子确保可重复性

            var data = Enumerable.Range(1, count).Select(x => new TestEntity()
            {
                Name = $"Entity_{x}",
                Value = x,
                Enum = (TestEntityEnum)(x % 3), // 平均分布三种枚举值
                Data = Enumerable.Range(1, 5).Select(i => x * i).ToArray(),
                DateTimeOffset = baseDateTime.AddHours(x % 8760), // 分布在一年内
                DateTime = baseDateTime.AddMinutes(x % 525600) // 分布在一年内
            });

            var sw = Stopwatch.StartNew();
            await DB.Collection<TestEntity>().InsertManyAsync(data, new InsertManyOptions()
            {
                BypassDocumentValidation = true,
            });
            sw.Stop();
            Debug.WriteLine($"Data insertion completed: {sw.ElapsedMilliseconds}ms for {count} records");
            sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(4000);

            await Task.Delay(1000); // 等待索引建立
        }

        private async Task TestBasicQueries()
        {
            Debug.WriteLine("\n=== Testing Basic Queries ===");
            var dbContext = new DbContext();
            var sw = Stopwatch.StartNew();

            try
            {
                // 基础查询所有记录
                sw.Restart();
                var allRecords = dbContext.Query<TestEntity>().AsNoTracking().ToList();
                sw.Stop();
                Debug.WriteLine($"Query all records (Query): {sw.ElapsedMilliseconds}ms, Count: {allRecords.Count}");
                sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(3000);

                // 使用Find查询所有记录
                sw.Restart();
                var allRecordsFind = await dbContext.Find<TestEntity>().ExecuteAsync();
                sw.Stop();
                Debug.WriteLine($"Query all records (Find): {sw.ElapsedMilliseconds}ms, Count: {allRecordsFind.Count}");
                sw.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(2000);

                // ID查询测试
                var idsToQuery = allRecords.Where((x, index) => index % 100 == 0).Select(x => x.Id).ToList();

                sw.Restart();
                foreach (var id in idsToQuery)
                {
                    dbContext.Query<TestEntity>().AsNoTracking().FirstOrDefault(x => x.Id == id).ShouldNotBeNull();
                }
                sw.Stop();
                Debug.WriteLine($"Query by ID (Query): {sw.ElapsedMilliseconds}ms for {idsToQuery.Count} queries");

                sw.Restart();
                foreach (var id in idsToQuery)
                {
                    (await dbContext.Find<TestEntity>().MatchId(id).ExecuteFirstAsync()).ShouldNotBeNull();
                }
                sw.Stop();
                Debug.WriteLine($"Query by ID (Find): {sw.ElapsedMilliseconds}ms for {idsToQuery.Count} queries");
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        private async Task TestComplexQueries()
        {
            Debug.WriteLine("\n=== Testing Complex Queries ===");
            var dbContext = new DbContext();
            var sw = Stopwatch.StartNew();

            try
            {
                // 范围查询
                sw.Restart();
                var rangeQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Value >= 10000 && x.Value <= 50000)
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Range query (Value 10000-50000): {sw.ElapsedMilliseconds}ms, Count: {rangeQuery.Count}");

                // 枚举值查询
                sw.Restart();
                var enumQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Enum == TestEntityEnum.Value1)
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Enum query (Value1): {sw.ElapsedMilliseconds}ms, Count: {enumQuery.Count}");

                // 字符串匹配查询
                sw.Restart();
                var stringQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Name.Contains("Entity_1"))
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"String contains query: {sw.ElapsedMilliseconds}ms, Count: {stringQuery.Count}");

                // 日期范围查询
                var startDate = DateTime.UtcNow.AddDays(-300);
                var endDate = DateTime.UtcNow.AddDays(-100);
                sw.Restart();
                var dateQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.DateTime >= startDate && x.DateTime <= endDate)
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Date range query: {sw.ElapsedMilliseconds}ms, Count: {dateQuery.Count}");

                // 复合条件查询
                sw.Restart();
                var complexQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Value > 50000 && x.Enum != TestEntityEnum.Default && x.Name.EndsWith("0"))
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Complex multi-condition query: {sw.ElapsedMilliseconds}ms, Count: {complexQuery.Count}");

                // 数组包含查询
                sw.Restart();
                var arrayQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Data.Contains(100))
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Array contains query: {sw.ElapsedMilliseconds}ms, Count: {arrayQuery.Count}");

                // 排序查询
                sw.Restart();
                var sortedQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .OrderByDescending(x => x.Value)
                    .Take(1000)
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Sorted query (Top 1000): {sw.ElapsedMilliseconds}ms, Count: {sortedQuery.Count}");

                // Find方式的复杂查询
                sw.Restart();
                var findComplexQuery = await dbContext.Find<TestEntity>()
                    .Match(x => x.Value > 100000 && x.Enum == TestEntityEnum.Value2)
                    .Sort(x => x.Descending(e => e.DateTime))
                    .Limit(5000)
                    .ExecuteAsync();
                sw.Stop();
                Debug.WriteLine($"Find complex query: {sw.ElapsedMilliseconds}ms, Count: {findComplexQuery.Count}");
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        private async Task TestAggregationQueries()
        {
            Debug.WriteLine("\n=== Testing Aggregation Queries ===");
            var dbContext = new DbContext();
            var sw = Stopwatch.StartNew();

            try
            {
                // 分组统计
                sw.Restart();
                var groupQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .GroupBy(x => x.Enum)
                    .Select(g => new { Enum = g.Key, Count = g.Count(), MaxValue = g.Max(x => x.Value) })
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Group by enum query: {sw.ElapsedMilliseconds}ms, Groups: {groupQuery.Count}");
                foreach (var group in groupQuery)
                {
                    Debug.WriteLine($"  Enum: {group.Enum}, Count: {group.Count}, MaxValue: {group.MaxValue}");
                }

                // 聚合统计
                sw.Restart();
                var avgValue = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Value < 100000)
                    .Average(x => x.Value);
                sw.Stop();
                Debug.WriteLine($"Average calculation: {sw.ElapsedMilliseconds}ms, Result: {avgValue:F2}");

                // 计数查询
                sw.Restart();
                var countQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Count(x => x.Enum == TestEntityEnum.Value1);
                sw.Stop();
                Debug.WriteLine($"Count query: {sw.ElapsedMilliseconds}ms, Count: {countQuery}");
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        private async Task TestProjectionQueries()
        {
            Debug.WriteLine("\n=== Testing Projection Queries ===");
            var dbContext = new DbContext();
            var sw = Stopwatch.StartNew();

            try
            {
                // 投影查询 - 选择部分字段
                sw.Restart();
                var projectionQuery = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Value < 10000)
                    .Select(x => new { x.Id, x.Name, x.Value, x.Enum })
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Projection query (partial fields): {sw.ElapsedMilliseconds}ms, Count: {projectionQuery.Count}");

                // 投影到自定义类型
                sw.Restart();
                var customProjection = dbContext.Query<TestEntity>()
                    .AsNoTracking()
                    .Where(x => x.Value >= 50000 && x.Value <= 60000)
                    .Select(x => new { x.Id, x.Name, x.Value, x.Enum })
                    .ToList()
                    .Select(x => new TestEntitySelectSubset(x.Id, x.Name, x.Value, x.Enum))
                    .ToList();
                sw.Stop();
                Debug.WriteLine($"Custom projection query: {sw.ElapsedMilliseconds}ms, Count: {customProjection.Count}");

                // Find方式的投影
                sw.Restart();
                var findProjection = await dbContext.Find<TestEntity>()
                    .Match(x => x.Enum == TestEntityEnum.Value2)
                    .Project(x => new { x.Name, x.Value, x.DateTime })
                    .Limit(1000)
                    .ExecuteAsync();
                sw.Stop();
                Debug.WriteLine($"Find projection query: {sw.ElapsedMilliseconds}ms, Count: {findProjection.Count}");
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        private async Task TestPaginationQueries()
        {
            Debug.WriteLine("\n=== Testing Pagination Queries ===");
            var dbContext = new DbContext();
            var sw = Stopwatch.StartNew();

            try
            {
                var pageSize = 1000;
                var totalPages = 10;

                // Skip/Take 分页测试
                var totalSkipTakeTime = 0L;
                for (int page = 0; page < totalPages; page++)
                {
                    sw.Restart();
                    var pageData = dbContext.Query<TestEntity>()
                        .AsNoTracking()
                        .OrderBy(x => x.Value)
                        .Skip(page * pageSize)
                        .Take(pageSize)
                        .ToList();
                    sw.Stop();
                    totalSkipTakeTime += sw.ElapsedMilliseconds;

                    if (page == 0 || page == totalPages - 1)
                    {
                        Debug.WriteLine($"Skip/Take page {page + 1}: {sw.ElapsedMilliseconds}ms, Count: {pageData.Count}");
                    }
                }
                Debug.WriteLine($"Total Skip/Take pagination time for {totalPages} pages: {totalSkipTakeTime}ms");

                // Find方式的分页测试
                var totalFindTime = 0L;
                for (int page = 0; page < totalPages; page++)
                {
                    sw.Restart();
                    var pageData = await dbContext.Find<TestEntity>()
                        .Sort(x => x.Ascending(e => e.Value))
                        .Skip(page * pageSize)
                        .Limit(pageSize)
                        .ExecuteAsync();
                    sw.Stop();
                    totalFindTime += sw.ElapsedMilliseconds;

                    if (page == 0 || page == totalPages - 1)
                    {
                        Debug.WriteLine($"Find pagination page {page + 1}: {sw.ElapsedMilliseconds}ms, Count: {pageData.Count}");
                    }
                }
                Debug.WriteLine($"Total Find pagination time for {totalPages} pages: {totalFindTime}ms");

                Debug.WriteLine($"Pagination performance comparison: Skip/Take vs Find = {totalSkipTakeTime}ms vs {totalFindTime}ms");
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }
}
