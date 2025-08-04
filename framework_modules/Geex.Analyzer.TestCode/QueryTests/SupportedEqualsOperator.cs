using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class SupportedEqualsOperatorTest
    {
        public void TestEqualsOperator()
        {
            var list = new List<TestEntity>();
            
            // == 运算符是支持的，不应该报告任何诊断
            var result = list.AsQueryable().Where(x => x.Name == "test");
        }
    }
}
