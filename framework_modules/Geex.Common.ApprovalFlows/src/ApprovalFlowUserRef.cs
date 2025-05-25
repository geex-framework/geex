using Geex.Abstractions;
using Geex.Abstractions.Storage;
using HotChocolate.Types;

using MongoDB.Bson.Serialization;

namespace Geex.Common.ApprovalFlows;

public class ApprovalFlowUserRef : Entity<ApprovalFlowUserRef>
{
    public ApprovalFlowUserRef()
    {

    }

    public ApprovalFlowUserRef(string approvalflowId, string userId, ApprovalFlowOwnershipType ownershipType)
    {
        ApprovalFlowId = approvalflowId;
        UserId = userId;
        OwnershipType = ownershipType;
    }
    public string ApprovalFlowId { get; set; }
    public string UserId { get; set; }
    public ApprovalFlowOwnershipType OwnershipType { get; set; }

    public class ApprovalFlowUserRefBsonConfig : BsonConfig<ApprovalFlowUserRef>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlowUserRef> map, BsonIndexConfig<ApprovalFlowUserRef> indexConfig)
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
