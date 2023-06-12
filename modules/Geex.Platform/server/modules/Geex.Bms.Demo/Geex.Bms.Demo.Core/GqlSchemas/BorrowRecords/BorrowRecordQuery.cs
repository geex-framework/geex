using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Types;

using HotChocolate.Types;

using MongoDB.Entities;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Types;
using Geex.Bms.Demo.Core.GqlSchemas.BorrowRecords.Inputs;

namespace Geex.Bms.Demo.Books.Core.Operations.Abstractions
{
    /// <summary>
    /// Book相关查询
    /// </summary>
    public class BorrowRecordQuery : QueryExtension<BorrowRecordQuery>
    {
        private readonly DbContext _dbContext;

        public BorrowRecordQuery(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        protected override void Configure(IObjectTypeDescriptor<BorrowRecordQuery> descriptor)
        {

            descriptor.Field(x => x.BorrowRecords(default))
                .UseOffsetPaging<BorrowRecordGqlType>()
                .UseFiltering<BorrowRecord>()
                .UseSorting<BorrowRecord>()
                ;
            base.Configure(descriptor);
        }

        public async Task<IQueryable<BorrowRecord>> BorrowRecords(QueryBorrowRecordInput input)
        {
            return _dbContext.Queryable<BorrowRecord>();
        }
    }
}
