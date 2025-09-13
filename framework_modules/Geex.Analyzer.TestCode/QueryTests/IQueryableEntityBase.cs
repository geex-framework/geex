using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class IQueryableEntityBaseTest
    {
        public void TestIQueryableEntityBase()
        {
            var list = new List<TestEntity>();
            
            // 对IQueryable<实体类>使用不支持的方法应该报告诊断
            var result = list.AsQueryable()
                .Where(x => x.Name.GetHashCode() > 100) // 这里应该报告GEEX003诊断
                .ToList();
        }
    }
}
