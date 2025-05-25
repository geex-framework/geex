using Geex.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowUserRef
{
    public class ApprovalFlowUserRefBsonConfig : BsonConfig<ApprovalFlowUserRef>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlowUserRef> map,
            BsonIndexConfig<ApprovalFlowUserRef> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.ApprovalFlowId));
            indexConfig.MapIndex(x => x.Descending(o => o.UserId));
        }
    }

    public class ApprovalFlowUserRefGqlConfig : GqlConfig.Object<ApprovalFlowUserRef>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowUserRef> descriptor)
        {
            descriptor.ConfigEntity();
            //descriptor.Implements<InterfaceType<IApprovalFlowDate>>();
            //descriptor.Field(x=>x.Nodes).Type<ListType<ObjectType<ApprovalFlowNode>>>();
            base.Configure(descriptor);
        }
    }
}