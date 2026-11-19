using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowTemplate
{
    public class ApprovalFlowTemplateBsonConfig : BsonConfig<ApprovalFlowTemplate>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlowTemplate> map, BsonIndexConfig<ApprovalFlowTemplate> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
        }
    }

    public class ApprovalFlowTemplateGqlConfig : GqlConfig.Object<ApprovalFlowTemplate>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowTemplate> descriptor)
        {
            descriptor.ConfigEntity();
            //descriptor.Implements<InterfaceType<IApprovalFlowTemplateDate>>();
            base.Configure(descriptor);
        }
    }
}
