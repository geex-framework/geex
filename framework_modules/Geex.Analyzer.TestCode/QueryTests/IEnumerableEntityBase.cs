using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class IEnumerableEntityBaseTest
    {
        public void TestIEnumerableEntityBase()
        {
            var list = new List<TestEntity>();
            
            // 对IEnumerable<实体类>使用不支持的方法不应该报告诊断
            // 因为这不是IQueryable<实体类>
            var result = list
                .Where(x => x.Name.GetHashCode() > 100)
                .Select(x => x.CreatedDate.ToString())
                .ToList();
        }
    }
}
