using System.Collections.Generic;
using System.Linq;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class NonEntitySummaryTestModel
    {
        public List<decimal> Lines { get; set; } = [];

        public decimal TotalAmount => Lines.Sum();
    }
}
