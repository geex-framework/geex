using System.Security.Claims;
using Geex.Abstractions.Authentication;
using Geex.Abstractions;
using Geex.Common.Identity.Api;

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities.Interceptors;

using Volo.Abp.Modularity;

namespace Geex.Common.Identity.Core
{
    [DependsOn(typeof(IdentityApiModule),
        typeof(GeexCoreModule))]
    public class IdentityCoreModule : GeexModule<IdentityCoreModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<IDataFilter<IOrgFilteredEntity>, OrgDataFilter>(x => new OrgDataFilter(x.GetService<ICurrentUser>()));
            base.ConfigureServices(context);
        }

    }
}
