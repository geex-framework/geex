using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class AllUnsupportedMethodsTest
    {
        public void TestAllUnsupportedMethods()
        {
            var list = new List<TestEntity>();
            var searchTerm = "dynamic";

            // 测试所有不支持的方法和属性
            var result = list.AsQueryable()
                // 不支持的方法 (GEEX003)
                .Where(x => x.Name.GetHashCode() > 100)
                .Where(x => x.GetType() == typeof(TestEntity))
                .Where(x => x.Name.Equals("test"))
                .Where(x => ReferenceEquals(x.Name, "test"))

                // 字符串方法参数限制 (GEEX003)
                .Where(x => x.Name.StartsWith(searchTerm))
                .Where(x => x.Name.EndsWith(searchTerm))
                .Where(x => x.Name.Contains(searchTerm))

                // 不支持的属性 (GEEX004)
                .Where(x => x.CreatedDate.Ticks > 0)
                .Where(x => x.LastModified.LocalDateTime > DateTime.Now)
                .Where(x => x.LastModified.UtcDateTime < DateTime.UtcNow)
                .Where(x => x.Duration.Days > 1)
                .Where(x => x.Duration.Hours < 24)
                .Where(x => x.Duration.Minutes == 30)

                // DateTime/DateTimeOffset 属性在 $match 阶段 (GEEX005)
                .Where(x => x.CreatedDate.Year == 2023)
                .Where(x => x.CreatedDate.Month > 6)
                .Where(x => x.CreatedDate.Day < 15)
                .Where(x => x.CreatedDate.Kind == DateTimeKind.Utc)
                .Where(x => x.LastModified.Offset.Hours > 0)
                .Where(x => x.LastModified.Offset.Minutes == 0);
        }
    }
}
