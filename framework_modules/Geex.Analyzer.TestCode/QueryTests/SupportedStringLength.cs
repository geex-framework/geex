using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class SupportedStringLengthTest
    {
        public void TestStringLength()
        {
            var list = new List<TestEntity>();
            
            // String.Length 属性在 Select 中是支持的
            var result = list.AsQueryable().Select(x => new { Name = x.Name, Length = x.Name.Length });
        }
    }
}
