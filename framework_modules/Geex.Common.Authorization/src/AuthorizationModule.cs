using System.Threading.Tasks;
using Geex.Common.Abstractions;
using Geex.Common.Authentication;
using Geex.Common.Authorization.Casbin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.Authorization
{
    [DependsOn(
        typeof(AuthenticationModule)
    )]
    public class AuthorizationModule : GeexModule<AuthorizationModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddCasbinAuthorization();
            base.ConfigureServices(context);
        }

        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
