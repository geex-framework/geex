using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Migrations;
using Geex.MultiTenant;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Identity.Migrations;

public class _638975042000000000_migrate_user_external_login : DbMigration
{
    public override long Number => long.Parse(GetType().Name.Split('_')[1]);

    public override async Task UpgradeAsync(IUnitOfWork uow)
    {
        _ = DB.Collection<UserExternalLogin>();

        var users = uow.DbContext.DefaultDb.GetCollection<BsonDocument>("User");
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Exists("OpenId"),
            Builders<BsonDocument>.Filter.Ne("OpenId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Ne("OpenId", ""),
            Builders<BsonDocument>.Filter.Exists("LoginProvider"),
            Builders<BsonDocument>.Filter.Ne("LoginProvider", nameof(LoginProviderEnum.Local)));

        var docs = await users.Find(uow.DbContext.Session, filter).ToListAsync();
        foreach (var doc in docs)
        {
            var userId = doc["_id"].AsString;
            var openId = doc["OpenId"].AsString;
            var loginProvider = LoginProviderEnum.FromValue(doc["LoginProvider"].AsString);
            var externalLogin = new UserExternalLogin(userId, loginProvider, openId, uow: uow);
            if (doc.TryGetValue("TenantCode", out var tenantCode) && tenantCode != BsonNull.Value)
            {
                var tenantCodeValue = tenantCode.AsString;
                if (!string.IsNullOrEmpty(tenantCodeValue))
                {
#pragma warning disable CS0618
                    externalLogin.As<ITenantFilteredEntity>().SetTenant(tenantCodeValue);
#pragma warning restore CS0618
                }
            }
        }

        if (docs.Count > 0)
        {
            await uow.SaveChanges();
            var unsetUpdate = Builders<BsonDocument>.Update
                .Unset("OpenId")
                .Unset("LoginProvider");
            await users.UpdateManyAsync(uow.DbContext.Session, filter, unsetUpdate);
        }
    }
}
