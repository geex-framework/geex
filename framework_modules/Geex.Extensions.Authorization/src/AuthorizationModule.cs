using Geex.Extensions.Authentication;
using Geex.Extensions.Authorization.Core.Casbin;
using Geex.Extensions.Authorization.Core.Utils;
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
            services.AddCasbinAuthorization(out var configureAction);
            SchemaBuilder.AddAuthorization(configureAction);
            SchemaBuilder.TryAddTypeInterceptor<AuthorizationTypeInterceptor>();
            base.ConfigureServices(context);
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            base.OnPreApplicationInitialization(context);
        }
    }
}
