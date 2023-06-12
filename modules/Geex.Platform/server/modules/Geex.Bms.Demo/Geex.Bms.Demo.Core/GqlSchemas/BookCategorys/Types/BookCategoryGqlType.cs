using Geex.Bms.Demo.Core.Aggregates.books;
using HotChocolate.Types;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Types
{
    public class BookCategoryGqlType : ObjectType<BookCategory>
    {
        protected override void Configure(IObjectTypeDescriptor<BookCategory> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
