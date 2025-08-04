using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedTimeSpanPropertyTest
    {
        public void TestTimeSpanProperties()
        {
            var list = new List<TestEntity>();
            
            // TimeSpan 属性在 Where 中可能不受支持 - 应该报告 GEEX004
            var result = list.AsQueryable()
                .Where(x => x.Duration.Days > 1)
                .Where(x => x.Duration.Hours < 24)
                .Where(x => x.Duration.Minutes == 30);
        }
    }
}
