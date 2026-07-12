using System.Threading.Tasks;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Identity.Migrations;

public class _638980000000000000_tenant_filter_user_external_login : DbMigration
{
    public override long Number => long.Parse(GetType().Name.Split('_')[1]);

    public override async Task UpgradeAsync(IUnitOfWork uow)
    {
        var collection = DB.Collection<UserExternalLogin>();
        var indexCursor = await collection.Indexes.ListAsync(uow.DbContext.Session, new ListIndexesOptions());
        var indexes = await indexCursor.ToListAsync();
        foreach (var index in indexes)
        {
            if (!index.TryGetValue("name", out var nameValue))
            {
                continue;
            }

            var name = nameValue.AsString;
            if (name == "_id_")
            {
                continue;
            }

            if (!index.TryGetValue("key", out var key) || key is not BsonDocument)
            {
                continue;
            }

            var keyDoc = (BsonDocument)key;
            var hasLoginProvider = keyDoc.Contains("LoginProvider");
            var hasLoginProviderId = keyDoc.Contains("LoginProviderId");
            var hasTenantCode = keyDoc.Contains("TenantCode");
            if (hasLoginProvider && hasLoginProviderId && !hasTenantCode)
            {
                await collection.Indexes.DropOneAsync(uow.DbContext.Session, name);
            }
        }

        var externalLogins = uow.DbContext.DefaultDb.GetCollection<BsonDocument>("UserExternalLogin");
        var users = uow.DbContext.DefaultDb.GetCollection<BsonDocument>("User");
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("TenantCode", false),
            Builders<BsonDocument>.Filter.Eq("TenantCode", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("TenantCode", ""));

        var docs = await externalLogins.Find(uow.DbContext.Session, filter).ToListAsync();
        foreach (var doc in docs)
        {
            var userId = doc.GetValue("UserId", default(BsonValue))?.AsString;
            if (string.IsNullOrEmpty(userId))
            {
                continue;
            }

            var user = await users.Find(uow.DbContext.Session, Builders<BsonDocument>.Filter.Eq("_id", userId))
                .FirstOrDefaultAsync();
            if (user == null)
            {
                continue;
            }

            var tenantCode = user.GetValue("TenantCode", BsonNull.Value);
            if (tenantCode == BsonNull.Value)
            {
                continue;
            }

            await externalLogins.UpdateOneAsync(
                uow.DbContext.Session,
                Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]),
                Builders<BsonDocument>.Update.Set("TenantCode", tenantCode));
        }
    }
}
