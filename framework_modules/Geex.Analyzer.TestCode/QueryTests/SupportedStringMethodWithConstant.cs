using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class SupportedStringMethodWithConstantTest
    {
        public void TestStringMethodsWithConstantParameters()
        {
            var list = new List<TestEntity>();
            
            // 在 Where 子句中使用常量参数的字符串方法是支持的
            var result = list.AsQueryable()
                .Where(x => x.Name.StartsWith("test"))
                .Where(x => x.Name.EndsWith("user"))
                .Where(x => x.Name.Contains("admin"));
        }
    }
}
