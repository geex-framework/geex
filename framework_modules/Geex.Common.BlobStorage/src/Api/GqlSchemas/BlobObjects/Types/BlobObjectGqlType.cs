using System;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;

using HotChocolate;
using HotChocolate.Types;
using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Api.GqlSchemas.BlobObjects.Types
{
    public class BlobObjectGqlType : ObjectType<BlobObject>
    {
        protected override void Configure(IObjectTypeDescriptor<BlobObject> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<IBlobObject>>();
            descriptor.ConfigEntity();
            //descriptor.Field(nameof(Url)).Resolver((context, token) => this.Url(context.Parent<IBlobObject>(), context.Service<BlobStorageModuleOptions>().FileDownloadPath));
            base.Configure(descriptor);
        }
        //public virtual string Url([Parent] IBlobObject parent)
        //{
        //    if (parent.StorageType == BlobStorageType.Db)
        //    {
        //        return "";
        //    }
        //    throw new NotImplementedException();
        //}
    }
}
