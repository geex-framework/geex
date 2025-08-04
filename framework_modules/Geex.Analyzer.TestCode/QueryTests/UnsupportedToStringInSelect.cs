using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedToStringInSelectTest
    {
        public void TestToStringInSelect()
        {
            var list = new List<TestEntity>();
            var result = list.AsQueryable().Select(x => x.Age.ToString());
        }
    }
}
