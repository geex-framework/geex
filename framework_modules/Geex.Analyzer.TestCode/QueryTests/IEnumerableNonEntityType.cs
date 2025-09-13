using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class IEnumerableNonEntityTypeTest
    {
        public void TestIEnumerableNonEntityType()
        {
            var list = new List<RegularClass>();
            
            // 对IEnumerable<非实体类>使用不支持的方法不应该报告诊断
            // 因为这既不是IQueryable，也不是实体类
            var result = list
                .Where(x => x.Name.GetHashCode() > 100)
                .Select(x => x.Date.ToString())
                .ToList();
        }
    }
}
