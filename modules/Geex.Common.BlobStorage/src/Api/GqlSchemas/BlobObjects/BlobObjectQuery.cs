using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects.Inputs;
using Geex.Common.BlobStorage.Api.GqlSchemas.BlobObjects.Types;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using HotChocolate;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Api.GqlSchemas.BlobObjects
{
    public class BlobObjectQuery : QueryExtension<BlobObjectQuery>
    {
        private readonly IMediator _mediator;

        public BlobObjectQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<BlobObjectQuery> descriptor)
        {
            descriptor.Field(x => x.BlobObjects())
            .UseOffsetPaging<BlobObjectGqlType>()
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

        /// <summary>
        /// 列表获取BlobObject
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IBlobObject>> BlobObjects(
            )
        {
            var result = await _mediator.Send(new QueryInput<IBlobObject>());
            return result;
        }

    }
}
