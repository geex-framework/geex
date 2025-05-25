using Geex.Abstractions;
using Geex.Abstractions.Authentication;
using Geex.Common.Identity.Core;
using Geex.Common.Identity.Core.Entities;
using Geex.Common.Identity.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities.Interceptors;
using Volo.Abp.Modularity;

namespace Geex.Common.Identity
{
    [DependsOn(typeof(GeexCoreModule))]
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
