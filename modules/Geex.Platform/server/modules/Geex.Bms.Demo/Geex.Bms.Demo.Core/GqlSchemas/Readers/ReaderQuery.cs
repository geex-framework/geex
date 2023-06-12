using System;
using System.Linq;
using System.Threading.Tasks;
using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Types;
using Geex.Bms.Demo.Core.GqlSchemas.Readers.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using HotChocolate.Types;
using MongoDB.Entities;

namespace Geex.Bms.Demo.Books.Core.Operations.Abstractions
{
    /// <summary>
    /// Book相关查询
    /// </summary>
    public class ReaderQuery:  QueryExtension<ReaderQuery>
    {
        private readonly DbContext _dbContext;

        public ReaderQuery(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        protected override void Configure(IObjectTypeDescriptor<ReaderQuery> descriptor)
        {
            descriptor.Field(x => x.Readers(default))
                .UseOffsetPaging<ReaderGqlType>()
                .UseFiltering<Reader>()
                .UseSorting<Reader>()
                ;
            base.Configure(descriptor);
        }

        public async Task<IQueryable<Reader>> Readers(QueryReaderInput input)
        {
            var result = _dbContext.Queryable<Reader>()
                .WhereIf(!input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name));
            return result;
        }

        public Reader ReaderById(string id)
        {
            var result = _dbContext.Queryable<Reader>().FirstOrDefault(x => x.Id == id);
            return result;
        }
    }
}
