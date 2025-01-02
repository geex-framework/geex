using Geex.Common.Abstraction;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.ApprovalFlows;

public partial class ApprovalFlowNode
{
    public class ApprovalFlowNodeBsonConfig : BsonConfig<ApprovalFlowNode>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlowNode> map, BsonIndexConfig<ApprovalFlowNode> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.ApprovalFlowId));
            indexConfig.MapIndex(x => x.Descending(o => o.AuditUserId));
        }
    }

    public class ApprovalFlowNodeGqlConfig : GqlConfig.Object<ApprovalFlowNode>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowNode> descriptor)
        {
            descriptor.ConfigEntity();
            //descriptor.Implements<InterfaceType<IApprovalFlowNodeData>>();
            base.Configure(descriptor);
        }
    }
}
