using HotChocolate.Types;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books.Types
{
    public class BorrowRecordGqlType : ObjectType<BorrowRecord>
    {
        protected override void Configure(IObjectTypeDescriptor<BorrowRecord> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
