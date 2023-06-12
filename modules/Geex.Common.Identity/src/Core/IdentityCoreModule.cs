using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;

using Geex.Common.Abstractions;
using Geex.Common.Identity.Api;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoDB.Entities;
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
            context.Services.AddScoped<IDataFilter<IOrgFilteredEntity>, OrgDataFilter>(x => new OrgDataFilter(x.GetService<LazyService<ClaimsPrincipal>>()));
            base.ConfigureServices(context);
        }

    }
}
