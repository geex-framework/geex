using Geex.Bms.Demo.Core.Aggregates.books;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Inputs
{
    public class EditBookCategoryInput: IRequest<Unit>
    {

        public string Name { get; set; }

        public string Describe { get; set; }
    }
}
