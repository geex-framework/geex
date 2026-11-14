using System.Threading.Tasks;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Geex.Extensions.ApprovalFlows.Core.Migrations;

public class _639279272758575675_approval_flow_tags : DbMigration
{
    public override long Number => 639279272758575675;

    public override async Task UpgradeAsync(IUnitOfWork uow)
    {
        foreach (var name in new[] { nameof(ApprovalFlowTemplate), nameof(ApprovalFlow) })
        {
            var collection = uow.DbContext.DefaultDb.GetCollection<BsonDocument>(name);
            var filter = Builders<BsonDocument>.Filter.Exists("Categories")
                & Builders<BsonDocument>.Filter.Exists(nameof(ApprovalFlowTemplate.Tags), false);
            await collection.UpdateManyAsync(uow.DbContext.Session, filter,
                Builders<BsonDocument>.Update.Rename("Categories", nameof(ApprovalFlowTemplate.Tags)));
        }

        var settings = uow.DbContext.DefaultDb.GetCollection<BsonDocument>("Setting");
        await settings.UpdateManyAsync(uow.DbContext.Session,
            Builders<BsonDocument>.Filter.Eq("Name", "ApprovalFlowsCategories"),
            Builders<BsonDocument>.Update.Set("Name", ApprovalFlowSettings.Tags.Name));
    }
}
