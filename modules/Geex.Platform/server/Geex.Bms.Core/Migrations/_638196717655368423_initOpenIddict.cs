using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    public class _638196717655368423_initOpenIddict : DbMigration
    {
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            dbContext.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictMongoDbApplication>>().CreateAsync(new OpenIddictApplicationDescriptor()
            {
                ClientId = "bms",
                ClientSecret = "bms",
                DisplayName = "bms",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Phone,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Address,

                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.DeviceCode,
                    OpenIddictConstants.Permissions.GrantTypes.Implicit,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://dev.geex.com"),
                    new Uri("https://bms.dev.geex.com"),
                    new Uri("https://bms.api.dev.geex.com"),

                    new Uri("https://geex.com"),
                    new Uri("https://bms.geex.com"),
                    new Uri("https://bms.api.geex.com"),
                },
                RedirectUris =
                {
                    new Uri("https://dev.geex.com"),
                    new Uri("https://bms.dev.geex.com"),
                    new Uri("https://bms.api.dev.geex.com"),

                    new Uri("https://geex.com"),
                    new Uri("https://bms.geex.com"),
                    new Uri("https://bms.api.geex.com"),
                }
            });
            //await applications.InsertOneAsync(new OpenIddictMongoDbApplication()
            //{
            //    ClientId = "bms",
            //    ClientSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes("bms")),
            //    DisplayName = "bms",
            //    RedirectUris = new List<string>()
            //    {
            //        "https://dev.geex.com",
            //        "https://bms.dev.geex.com",
            //        "https://bms.api.dev.geex.com",

            //        "https://geex.com",
            //        "https://bms.geex.com",
            //        "https://bms.api.geex.com",
            //    },
            //    PostLogoutRedirectUris = new List<string>()
            //    {
            //        "https://dev.geex.com",
            //        "https://bms.dev.geex.com",
            //        "https://bms.api.dev.geex.com",

            //        "https://geex.com",
            //        "https://bms.geex.com",
            //        "https://bms.api.geex.com",
            //    },
            //    Permissions = new[]
            //    {
            //        OpenIddictConstants.Permissions.Scopes.Email,
            //        OpenIddictConstants.Permissions.Scopes.Phone,
            //        OpenIddictConstants.Permissions.Scopes.Roles,
            //        OpenIddictConstants.Permissions.Scopes.Profile,
            //        OpenIddictConstants.Permissions.Scopes.Address,

            //        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            //        OpenIddictConstants.Permissions.GrantTypes.DeviceCode,
            //        OpenIddictConstants.Permissions.GrantTypes.Implicit,
            //        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            //        OpenIddictConstants.Permissions.GrantTypes.Password,
            //    },
            //});
        }
    }
}
