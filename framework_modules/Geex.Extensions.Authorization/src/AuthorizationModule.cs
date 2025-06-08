using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authorization.Core.Casbin;
using Geex.Extensions.Authorization.Core.Utils;
using Geex.Gql;

using HotChocolate;
using HotChocolate.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using OpenIddict.Client.AspNetCore;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using AuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;

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
            SchemaBuilder.UseRequest(next => async context =>
            {
                var work = context.Services.GetService<IUnitOfWork>();
                if (work != null)
                {
                    if (context.Services.GetService<ClaimsPrincipal>()?.FindUserId() == GeexConstants.SuperAdminId)
                    {
                        work.DbContext.DisableAllDataFilters();
                    }
                }
                await next(context);
            });
            SchemaBuilder.AddAuthorization(configureAction);
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
