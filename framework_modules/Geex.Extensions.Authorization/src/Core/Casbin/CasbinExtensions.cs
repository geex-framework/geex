using System;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using OpenIddict.Validation.AspNetCore;

namespace Geex.Extensions.Authorization.Core.Casbin
{
    public static class CasbinExtensions
    {
        public static void AddCasbinAuthorization(this IServiceCollection services, out Action<AuthorizationOptions> configureAction)
        {
            // Replace the default authorization policy provider with our own
            // custom provider which can return authorization policies for given
            // policy names (instead of using the default policy provider)
            services.AddSingleton(x => new CasbinMongoAdapter(() => DB.Collection<CasbinRule>()));
            services.AddSingleton<RbacEnforcer>();
            services.AddSingleton<IRbacEnforcer, RbacEnforcer>();
            services.AddSingleton<IAuthorizationPolicyProvider, CasbinAuthorizationPolicyProvider>();

            //// As always, handlers must be provided for the requirements of the authorization policies
            services.AddSingleton<IAuthorizationHandler, CasbinAuthorizationHandler>();
            configureAction = x =>
            {
                x.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes("SuperAdmin",
                        "Local",
                        JwtBearerDefaults.AuthenticationScheme,
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        //OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        //OpenIddictClientAspNetCoreDefaults.AuthenticationScheme,
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            };
            services.AddAuthorization(configureAction);
        }
    }
}
