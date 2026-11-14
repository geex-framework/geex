using System.Threading.Tasks;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Geex.Extensions.ApprovalFlows.Core.Migrations;

public class _639288672498274456_approval_flow_closed_status : DbMigration
{
    public override long Number => 639288672498274456;

    public override async Task UpgradeAsync(IUnitOfWork uow)
    {
        var collection = uow.DbContext.DefaultDb.GetCollection<BsonDocument>(nameof(ApprovalFlow));
        var filter = Builders<BsonDocument>.Filter.In(nameof(ApprovalFlow.Status),
            new BsonValue[] { -1, "Canceled", "Archived" });
        await collection.UpdateManyAsync(uow.DbContext.Session, filter,
            Builders<BsonDocument>.Update.Set(nameof(ApprovalFlow.Status), (int)ApprovalFlowStatus.Closed));
    }
}
