using HotChocolate.Types;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books.Types
{
    // * BookGqlType��ʵ�嵽��Dtoһ��ӳ������.
    public class BookGqlType : ObjectType<Book>
    {
        protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
