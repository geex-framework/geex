using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Inputs;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Inputs
{
    public class QueryBookCategoryInput :QueryInput<BookCategory>
    {
        public string Name { get; set; }

    }
}
