using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Requests;

using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.BlobStorage.Core.GqlSchemas.BlobObjects
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
