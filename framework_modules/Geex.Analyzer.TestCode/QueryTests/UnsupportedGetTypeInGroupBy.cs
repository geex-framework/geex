using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedGetTypeInGroupByTest
    {
        public void TestGetTypeInGroupBy()
        {
            var list = new List<TestEntity>();
            
            // GetType 方法在 GroupBy 中不受支持 - 应该报告 GEEX003
            var result = list.AsQueryable().GroupBy(x => x.GetType());
        }
    }
}
