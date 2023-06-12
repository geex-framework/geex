using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs
{
    public class QueryBookInput:QueryInput<Book>
    {
        public string? Name { get; set; }
    }
}
