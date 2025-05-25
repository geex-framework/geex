using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Migrations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoDB.Driver;
using MongoDB.Entities;

using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace Geex.Common.Authentication.Migrations
{
    public class _638194860579784283_initOpenIddict : DbMigration
    {
        /// <inheritdoc />
        public override long Number => long.Parse(this.GetType().Name.Split('_')[1]);

        public override async Task UpgradeAsync(IUnitOfWork uow)
        {
            var database = uow.DbContext.DefaultDb;
            var options = uow.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenIddictMongoDbOptions>>().CurrentValue;
            var applications = database.GetCollection<OpenIddictMongoDbApplication>(options.ApplicationsCollectionName);

            await applications.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<OpenIddictMongoDbApplication>(
                    Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(application => application.ClientId),
                    new CreateIndexOptions
                    {
                        Unique = true
                    }),

                new CreateIndexModel<OpenIddictMongoDbApplication>(
                    Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(application => application.PostLogoutRedirectUris),
                    new CreateIndexOptions
                    {
                        Background = true
                    }),

                new CreateIndexModel<OpenIddictMongoDbApplication>(
                    Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(application => application.RedirectUris),
                new CreateIndexOptions
                {
                    Background = true
                    })
            });

            var authorizations = database.GetCollection<OpenIddictMongoDbAuthorization>(options.AuthorizationsCollectionName);

            await authorizations.Indexes.CreateOneAsync(
                new CreateIndexModel<OpenIddictMongoDbAuthorization>(
                    Builders<OpenIddictMongoDbAuthorization>.IndexKeys
                        .Ascending(authorization => authorization.ApplicationId)
                        .Ascending(authorization => authorization.Scopes)
                        .Ascending(authorization => authorization.Status)
                        .Ascending(authorization => authorization.Subject)
                        .Ascending(authorization => authorization.Type),
                        new CreateIndexOptions
                        {
                            Background = true
                        }));

            var scopes = database.GetCollection<OpenIddictMongoDbScope>(options.ScopesCollectionName);

            await scopes.Indexes.CreateOneAsync(new CreateIndexModel<OpenIddictMongoDbScope>(
                Builders<OpenIddictMongoDbScope>.IndexKeys.Ascending(scope => scope.Name),
                        new CreateIndexOptions
                        {
                            Unique = true
                        }));

            var tokens = database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName);

            await tokens.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(token => token.ReferenceId),
                    new CreateIndexOptions<OpenIddictMongoDbToken>
                    {
                        // Note: partial filter expressions are not supported on Azure Cosmos DB.
                        // As a workaround, the expression and the unique constraint can be removed.
                        PartialFilterExpression = Builders<OpenIddictMongoDbToken>.Filter.Exists(token => token.ReferenceId),
                        Unique = true
                    }),

                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys
                        .Ascending(token => token.ApplicationId)
                        .Ascending(token => token.Status)
                        .Ascending(token => token.Subject)
                        .Ascending(token => token.Type),
                    new CreateIndexOptions
                    {
                        Background = true
                    })
            });
        }
    }
}
