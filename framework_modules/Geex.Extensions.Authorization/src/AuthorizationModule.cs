using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authorization.Casbin;
using Geex.Gql;

using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Authorization
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
            SchemaBuilder.AddAuthorization();
            SchemaBuilder.TryAddTypeInterceptor<AuthorizationTypeInterceptor>();
            base.ConfigureServices(context);
        }

        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
