using Geex.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.ApprovalFlows;

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
