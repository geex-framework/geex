using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class SupportedDateTimePropertyTest
    {
        public void TestSupportedDateTimeProperties()
        {
            var list = new List<TestEntity>();
            
            // DateTime 属性在 Select 中是支持的
            var result = list.AsQueryable().Select(x => new 
            { 
                Year = x.CreatedDate.Year,
                Month = x.CreatedDate.Month,
                Day = x.CreatedDate.Day,
                Hour = x.CreatedDate.Hour,
                Minute = x.CreatedDate.Minute,
                Second = x.CreatedDate.Second
            });
        }
    }
}
