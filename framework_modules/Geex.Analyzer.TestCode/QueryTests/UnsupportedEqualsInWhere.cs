using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedEqualsInWhereTest
    {
        public void TestEqualsMethod()
        {
            var list = new List<TestEntity>();
            
            // Equals 方法在 Where 中不受支持 - 应该报告 GEEX003
            var result = list.AsQueryable().Where(x => x.Name.Equals("test"));
        }
    }
}
