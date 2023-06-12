using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Inputs;


namespace Geex.Bms.Demo.Core.GqlSchemas.BorrowRecords.Inputs
{
    public class QueryBorrowRecordInput  :QueryInput<Reader>
    {
        public string BookId { get; set; }
    }
}
