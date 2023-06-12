using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Inputs;


namespace Geex.Bms.Demo.Core.GqlSchemas.Readers.Inputs
{
    public class QueryReaderInput :QueryInput<Reader>
    {
        public string? Name { get; set; }
    }
}
