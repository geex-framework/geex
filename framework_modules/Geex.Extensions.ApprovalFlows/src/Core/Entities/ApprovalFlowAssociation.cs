using Geex.Storage;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public class ApprovalFlowAssociation : Entity<ApprovalFlowAssociation>
{
    public string Key { get; set; } = "";
    public string FlowId { get; set; } = "";
    public string PreviousFlowId { get; set; } = "";

    public class Config : BsonConfig<ApprovalFlowAssociation>
    {
        protected override void Map(BsonClassMap<ApprovalFlowAssociation> map, BsonIndexConfig<ApprovalFlowAssociation> indexes)
        {
            map.AutoMap();
            indexes.MapIndex(x => x.Ascending(y => y.Key).Ascending(y => y.PreviousFlowId), options => options.Unique = true);
        }
    }
}
