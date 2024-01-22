using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.MultiTenant;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoDB.Entities;

using NetCasbin;
using NetCasbin.Abstractions;
using HotChocolate;
using MongoDB.Driver;

namespace Geex.Common.Authorization.Casbin
{
    public static class CasbinExtensions
    {
        public static void AddCasbinAuthorization(this IServiceCollection services)
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
            services.AddAuthorization(x =>
            {
                x.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            });
        }
    }
}