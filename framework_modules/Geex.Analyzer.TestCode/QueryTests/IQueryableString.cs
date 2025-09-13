using System.Collections.Generic;
using System.Linq;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class IQueryableStringTest
    {
        public void TestIQueryableString()
        {
            var list = new List<string> { "test1", "test2", "test3" };
            
            // 对IQueryable<string>使用不支持的方法不应该报告诊断
            // 因为string不是实体类
            var result = list.AsQueryable()
                .Where(x => x.GetHashCode() > 100)
                .Select(x => x.ToString())
                .ToList();
        }
    }
}
