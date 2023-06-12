using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Types;

using HotChocolate.Types;

using MongoDB.Entities;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Types;
using HotChocolate.Data.Sorting;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books
{

    public class BookQuery : QueryExtension<BookQuery>
    {
        private readonly DbContext _dbContext;

        public BookQuery(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        protected override void Configure(IObjectTypeDescriptor<BookQuery> descriptor)
        {

            descriptor.Field(x => x.Books(default))
            .UseOffsetPaging<BookGqlType>()
            .UseFiltering<Book>()
            .UseSorting<Book>(x=>x.Ignore(a=>a.Attachments).Ignore(a=>a.BookCategory).Ignore(a=>a.BorrowRecords))
            ;
            base.Configure(descriptor);
        }

        /// <summary>
        /// 列表获取book
        /// </summary>
        /// <returns></returns>
        public async Task<IQueryable<Book>> Books(QueryBookInput input)
        {
            var result = _dbContext.Queryable<Book>()
                .WhereIf(!input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name));
            return result;
        }

        /// <summary>
        /// 列表获取book
        /// </summary>
        /// <returns></returns>
        public async Task<Book> BookById(string id)
        {
            var result = _dbContext.Queryable<Book>().FirstOrDefault(x => x.Id == id);
            return result;
        }

    }
}
