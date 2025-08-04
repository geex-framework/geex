using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedStringMethodWithNonConstantTest
    {
        public void TestStringMethodsWithNonConstantParameters()
        {
            var list = new List<TestEntity>();
            var searchTerm = "dynamic";
            
            // 在 Where 子句中使用非常量参数的字符串方法 - 应该报告 GEEX003
            var result = list.AsQueryable()
                .Where(x => x.Name.StartsWith(searchTerm))
                .Where(x => x.Name.EndsWith(searchTerm))
                .Where(x => x.Name.Contains(searchTerm));
        }
    }
}
