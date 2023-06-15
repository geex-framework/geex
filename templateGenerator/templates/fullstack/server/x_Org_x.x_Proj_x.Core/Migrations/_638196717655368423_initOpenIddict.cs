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
                ClientId = "x_proj_x",
                ClientSecret = "x_proj_x",
                DisplayName = "x_proj_x",
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
                    new Uri("https://dev.x_org_x.com"),
                    new Uri("https://x_proj_x.dev.x_org_x.com"),
                    new Uri("https://x_proj_x.api.dev.x_org_x.com"),

                    new Uri("https://x_org_x.com"),
                    new Uri("https://x_proj_x.x_org_x.com"),
                    new Uri("https://x_proj_x.api.x_org_x.com"),
                },
                RedirectUris =
                {
                    new Uri("https://dev.x_org_x.com"),
                    new Uri("https://x_proj_x.dev.x_org_x.com"),
                    new Uri("https://x_proj_x.api.dev.x_org_x.com"),

                    new Uri("https://x_org_x.com"),
                    new Uri("https://x_proj_x.x_org_x.com"),
                    new Uri("https://x_proj_x.api.x_org_x.com"),
                }
            });
            //await applications.InsertOneAsync(new OpenIddictMongoDbApplication()
            //{
            //    ClientId = "x_proj_x",
            //    ClientSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes("x_proj_x")),
            //    DisplayName = "x_proj_x",
            //    RedirectUris = new List<string>()
            //    {
            //        "https://dev.x_org_x.com",
            //        "https://x_proj_x.dev.x_org_x.com",
            //        "https://x_proj_x.api.dev.x_org_x.com",

            //        "https://x_org_x.com",
            //        "https://x_proj_x.x_org_x.com",
            //        "https://x_proj_x.api.x_org_x.com",
            //    },
            //    PostLogoutRedirectUris = new List<string>()
            //    {
            //        "https://dev.x_org_x.com",
            //        "https://x_proj_x.dev.x_org_x.com",
            //        "https://x_proj_x.api.dev.x_org_x.com",

            //        "https://x_org_x.com",
            //        "https://x_proj_x.x_org_x.com",
            //        "https://x_proj_x.api.x_org_x.com",
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
