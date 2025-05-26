using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Autofac.Core;

using Geex.Abstractions;
using Geex.Abstractions.Authentication;

using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Domain;
using Geex.Extensions.Authentication.Utils;
using Geex.Extensions.Authorization;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.Identity.Core;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Core.Handlers;
using Geex.Extensions.Settings;

using HotChocolate;
using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

using OpenIddict.Abstractions;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Identity
{
    [DependsOn(typeof(AuthenticationModule), typeof(AuthorizationModule), typeof(BlobStorageModule), typeof(SettingsModule))]
    public class IdentityModule : GeexModule<IdentityModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<IDataFilter<IOrgFilteredEntity>, OrgDataFilter>(x => new OrgDataFilter(x.GetService<ICurrentUser>()));
            context.Services.AddTransient<IUserCreationValidator, UserCreationValidator>();
            context.Services.AddTransient<UserHandler>();
            context.Services.AddTransient<OrgHandler>();
            context.Services.AddTransient<RoleHandler>();
            base.ConfigureServices(context);
        }
    }
}
