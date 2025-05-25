using Geex.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlow
{
    public class ApprovalFlowBsonConfig : BsonConfig<ApprovalFlow>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlow> map, BsonIndexConfig<ApprovalFlow> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.CreatorUserId));
        }
    }

    public class ApprovalFlowGqlConfig : GqlConfig.Object<ApprovalFlow>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlow> descriptor)
        {
            descriptor.ConfigEntity();
            //descriptor.Field(x=>x.Nodes).Type<ListType<ObjectType<ApprovalFlowNode>>>();
            //descriptor.Field(x=>x.Stakeholders).Type<ListType<ObjectType<ApprovalFlowUserRef>>>();
            //descriptor.Implements<InterfaceType<IApprovalFlowDate>>();
            //descriptor.Field(x=>x.Nodes).Type<ListType<ObjectType<ApprovalFlowNode>>>();
            base.Configure(descriptor);
        }
    }
}
