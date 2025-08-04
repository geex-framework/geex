using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class ComplexQueryExpressionsTest
    {
        public void TestComplexQueryExpressions()
        {
            var list = new List<TestEntity>();
            
            // 复杂但支持的查询表达式
            var result = list.AsQueryable()
                .Where(x => x.Age > 18 && x.Name.Length > 3)
                .Where(x => x.Category == "Admin" || x.Category == "User")
                .Select(x => new 
                { 
                    Name = x.Name,
                    Age = x.Age,
                    Year = x.CreatedDate.Year,
                    CategoryUpper = x.Category.ToUpper()
                })
                .Where(x => x.Age < 65)
                .OrderBy(x => x.Name)
                .ThenByDescending(x => x.Age);
        }

        public void TestNestedQueryExpressions()
        {
            var list = new List<TestEntity>();
            
            // 嵌套查询表达式
            var result = list.AsQueryable()
                .Where(x => x.Name.StartsWith("A") && 
                           (x.Age > 20 || x.Category == "VIP"))
                .GroupBy(x => x.Category)
                .Select(g => new 
                { 
                    Category = g.Key,
                    Count = g.Count(),
                    AverageAge = g.Average(x => x.Age)
                });
        }
    }
}
