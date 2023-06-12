using Geex.Bms.Demo.Core.Aggregates.books;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Inputs
{
    public class CreateBookCategoryInput: IRequest<BookCategory>
    {
        public string Name { get; set; }

        public string Describe { get; set; }
    }
}
