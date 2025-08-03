using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authorization;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.Identity.Core;
using Geex.Extensions.Identity.Core.Handlers;
using Geex.Extensions.Identity.Utils;
using Geex.Extensions.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Entities.Interceptors;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Identity
{
    [DependsOn(typeof(AuthenticationModule), typeof(AuthorizationModule), typeof(BlobStorageModule), typeof(SettingsModule))]
    public class IdentityModule : GeexModule<IdentityModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<IDataFilter<IOrgFilteredEntity>, OrgDataFilter>(x => new OrgDataFilter(x.GetService<ICurrentUser>()));
            context.Services.AddTransient<IPasswordHasher<IUser>, PasswordHasher<IUser>>();
            context.Services.AddTransient<IUserCreationValidator, UserCreationValidator>();
            context.Services.AddTransient<UserHandler>();
            context.Services.AddTransient<OrgHandler>();
            context.Services.AddTransient<RoleHandler>();
            context.Services.TryAddEnumerable(new ServiceDescriptor(typeof(ISubClaimsTransformation), typeof(IdentitySubClaimsTransformation), ServiceLifetime.Singleton));
            base.ConfigureServices(context);
        }
    }
}
