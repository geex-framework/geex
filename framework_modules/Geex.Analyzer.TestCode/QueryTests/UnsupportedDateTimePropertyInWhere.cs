using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedDateTimePropertyInWhereTest
    {
        public void TestDateTimePropertiesInWhere()
        {
            var list = new List<TestEntity>();
            
            // DateTime 属性在 Where 中使用时应该报告 GEEX005
            var result = list.AsQueryable()
                .Where(x => x.CreatedDate.Year == 2023)
                .Where(x => x.CreatedDate.Month > 6)
                .Where(x => x.CreatedDate.Day < 15)
                .Where(x => x.CreatedDate.Kind == System.DateTimeKind.Utc);
        }
    }
}
