using Geex.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.ApprovalFlows;

public partial class ApprovalFlowNodeLog
{
    public class ApprovalFlowNodeLogBsonConfig : BsonConfig<ApprovalFlowNodeLog>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlowNodeLog> map, BsonIndexConfig<ApprovalFlowNodeLog> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.ApprovalFlowNodeId));
        }
    }

    public class ApprovalFlowNodeLogGqlConfig : GqlConfig.Object<ApprovalFlowNodeLog>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowNodeLog> descriptor)
        {
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
