using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedDateTimeTicksTest
    {
        public void TestDateTimeTicks()
        {
            var list = new List<TestEntity>();
            
            // DateTime.Ticks 在 OrderBy 中不受支持 - 应该报告 GEEX004
            var result = list.AsQueryable().OrderBy(x => x.CreatedDate.Ticks);
        }
    }
}
