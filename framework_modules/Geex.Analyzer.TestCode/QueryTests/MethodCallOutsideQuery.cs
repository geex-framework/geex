using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class MethodCallOutsideQueryTest
    {
        public void TestMethodCallOutsideQuery()
        {
            var list = new List<TestEntity>();
            var entity = new TestEntity { Name = "test" };
            
            // 查询外的方法调用不应该报告诊断
            var hashCode = entity.Name.GetHashCode();
            var stringValue = entity.CreatedDate.ToString();
            var typeInfo = entity.GetType();
            
            // 只有查询内的才需要检查
            var result = list.AsQueryable().Where(x => x.Age > 18);
        }
    }
}
