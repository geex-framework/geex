using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedDateTimeTimeOfDayTest
    {
        public void TestDateTimeTimeOfDay()
        {
            var list = new List<TestEntity>();
            
            // DateTime.TimeOfDay 在 Select 中不受支持 - 应该报告 GEEX004
            var result = list.AsQueryable().Select(x => x.CreatedDate.TimeOfDay);
        }
    }
}
