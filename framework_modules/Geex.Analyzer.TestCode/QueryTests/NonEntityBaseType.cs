using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class NonEntityBaseTypeTest
    {
        public void TestNonEntityQuery()
        {
            var list = new List<RegularClass>();
            
            // 对非实体类型的查询不应该报告任何诊断
            var result = list.AsQueryable()
                .Where(x => x.Name.GetHashCode() > 100)
                .Select(x => x.Date.ToString());
        }
    }
}
