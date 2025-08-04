using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedDateTimeOffsetPropertyTest
    {
        public void TestDateTimeOffsetProperties()
        {
            var list = new List<TestEntity>();
            
            // DateTimeOffset 属性在 Where 中使用时应该报告 GEEX005
            var result = list.AsQueryable()
                .Where(x => x.LastModified.Offset.Hours > 0)
                .Where(x => x.LastModified.Offset.Minutes == 0);
        }
    }
}
