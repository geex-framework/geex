using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Types;

using HotChocolate.Types;

using MongoDB.Entities;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Types;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Types;
using Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Inputs;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys
{
    /// <summary>
    /// Book相关查询
    /// </summary>
    public class BookCategoryQuery: QueryExtension<BookCategoryQuery>
    {
        private readonly DbContext _dbContext;

        public BookCategoryQuery(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        protected override void Configure(IObjectTypeDescriptor<BookCategoryQuery> descriptor)
        {
            descriptor.Field(x => x.BookCategorys(default))
                .UseOffsetPaging<BookCategoryGqlType>()
                .UseFiltering<BookCategory>()
                .UseSorting<BookCategory>();

            base.Configure(descriptor);
        }

        public async Task<IQueryable<BookCategory>> BookCategorys(QueryBookCategoryInput input)
        {
            return _dbContext.Queryable<BookCategory>()
                .WhereIf(!input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name));;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BookCategory> BookCategoryById(string id)
        {
            var result = _dbContext.Queryable<BookCategory>().FirstOrDefault(x => x.Id == id);
            return result;
        }
    }
}
