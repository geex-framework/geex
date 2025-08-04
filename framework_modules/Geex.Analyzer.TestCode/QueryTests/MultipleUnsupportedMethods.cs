using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class MultipleUnsupportedMethodsTest
    {
        public void TestMultipleUnsupportedMethods()
        {
            var list = new List<TestEntity>();
            
            // 多个不支持的方法应该报告多个诊断
            var result = list.AsQueryable()
                .Where(x => x.Name.GetHashCode() > 100)  // GEEX003
                .Select(x => x.CreatedDate.ToString());  // GEEX003
        }
    }
}
