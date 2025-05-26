using System.Linq;
using System.Threading.Tasks;

using Geex.Abstractions;

using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.BlobStorage.GqlSchemas.BlobObjects
{
    public sealed class BlobObjectQuery : QueryExtension<BlobObjectQuery>
    {

        protected override void Configure(IObjectTypeDescriptor<BlobObjectQuery> descriptor)
        {
            descriptor.Field(x => x.BlobObjects())
            .UseOffsetPaging<InterfaceType<IBlobObject>>()
            .UseFiltering<IBlobObject>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.Md5);
                x.Field(y => y.MimeType);
                x.Field(y => y.StorageType);
                x.Field(y => y.FileSize);
                x.Field(y => y.FileName);
            })
            ;
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public BlobObjectQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }


        /// <summary>
        /// 列表获取BlobObject
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IBlobObject>> BlobObjects()
        {
            var result = await _uow.Request(new QueryRequest<IBlobObject>());
            return result;
        }

    }
}
