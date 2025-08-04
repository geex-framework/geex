using System.Collections.Generic;
using System.Linq;
using Geex.Analyzer.TestCode.QueryTests;

namespace Geex.Analyzer.TestCode.QueryTests
{
    public class UnsupportedGetHashCodeInWhereTest
    {
        public void TestGetHashCode()
        {
            var list = new List<TestEntity>();
            var result = list.AsQueryable().Where(x => x.Name.GetHashCode() > 100);
        }
    }
}
