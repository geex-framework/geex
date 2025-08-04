using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class SupportedTimeSpanPropertyTest
    {
        public void TestSupportedTimeSpanProperties()
        {
            var list = new List<TestEntity>();
            
            // TimeSpan 属性在 Select 中是支持的
            var result = list.AsQueryable().Select(x => new 
            { 
                TotalDays = x.Duration.TotalDays,
                TotalHours = x.Duration.TotalHours,
                TotalMinutes = x.Duration.TotalMinutes,
                Ticks = x.Duration.Ticks
            });
        }
    }
}
