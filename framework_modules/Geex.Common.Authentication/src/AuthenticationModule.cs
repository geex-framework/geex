using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstractions;
using Geex.Common.Abstractions.Enumerations;
using Geex.Common.Authentication.Domain;
using Geex.Common.Authentication.Utils;

using HotChocolate;
using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;
using MongoDB.Entities;

using OpenIddict.Abstractions;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.Authentication
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class AuthenticationModule : GeexModule<AuthenticationModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            IdentityModelEventSource.ShowPII = true;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var services = context.Services;
            services.AddTransient<IPasswordHasher<IUser>, PasswordHasher<IUser>>();
            var moduleOptions = services.GetSingletonInstance<AuthenticationModuleOptions>();

            services.AddSingleton<GeexJwtSecurityTokenHandler>();
            var authenticationBuilder = services
               .AddAuthentication("SuperAdmin");

            if (moduleOptions.InternalAuthOptions != default)
            {
                var authOptions = moduleOptions.InternalAuthOptions;
                var tokenValidationParameters = new TokenValidationParameters
                {
                    // 签名键必须匹配!
                    ValidateIssuerSigningKey = !authOptions.SecurityKey.IsNullOrEmpty(),
                    IssuerSigningKey = authOptions.SecurityKey.IsNullOrEmpty() ? default : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SecurityKey)),

                    // 验证JWT发行者(iss)的 claim
                    ValidateIssuer = !authOptions.ValidIssuer.IsNullOrEmpty(),
                    ValidIssuer = authOptions.ValidIssuer,

                    // Validate the JWT Audience (aud) claim
                    ValidateAudience = !authOptions.ValidAudience.IsNullOrEmpty(),
                    ValidAudience = authOptions.ValidAudience,

                    // 验证过期
                    ValidateLifetime = authOptions.TokenExpireInSeconds > 0,

                    // If you want to allow a certain amount of clock drift, set that here
                    ClockSkew = TimeSpan.Zero,
                };
                services.AddSingleton<TokenValidationParameters>(tokenValidationParameters);

                void ConfigJwtBearerOptions(JwtBearerOptions jwtBearerOptions)
                {
                    jwtBearerOptions.TokenValidationParameters = tokenValidationParameters;
                    jwtBearerOptions.SecurityTokenValidators.Clear();
                    jwtBearerOptions.SecurityTokenValidators.Add(services.GetRequiredServiceLazy<GeexJwtSecurityTokenHandler>().Value);
                    jwtBearerOptions.Events ??= new JwtBearerEvents();
                    jwtBearerOptions.Events.OnMessageReceived = receivedContext =>
                    {
                        if (receivedContext.HttpContext.WebSockets.IsWebSocketRequest)
                        {
                            if (receivedContext.HttpContext.Items.TryGetValue("jwtToken", out var token1))
                            {
                                receivedContext.Token = token1.ToString();
                            }
                            else if (receivedContext.Request.Query.TryGetValue("access_token", out var token2))
                            {
                                receivedContext.Token = token2.ToString();
                            }
                        }
                        return Task.CompletedTask;
                    };
                }

                authenticationBuilder
                    .AddScheme<JwtBearerOptions, LocalAuthHandler>("Bearer", "Bearer", ConfigJwtBearerOptions)
                    .AddCookie()
                    .AddScheme<JwtBearerOptions, LocalAuthHandler>("Local", "Local", ConfigJwtBearerOptions)
                    .AddScheme<AuthenticationSchemeOptions, SuperAdminAuthHandler>("SuperAdmin", "SuperAdmin", (o =>
                    {
                        o.ForwardDefaultSelector = httpContext =>
                        {
                            var schema = httpContext.Request.Headers.Authorization.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                            return schema ?? "Local";
                        };
                    }));
                services.AddSingleton(new UserTokenGenerateOptions(authOptions.ValidIssuer, authOptions.ValidAudience, authOptions.SecurityKey, TimeSpan.FromSeconds(authOptions.TokenExpireInSeconds)));
                services.AddScoped<IClaimsTransformation, GeexClaimsTransformation>();

                services.AddSingleton<IdsvrMiddleware>();

                services.AddSingleton<ISocketSessionInterceptor, SubscriptionAuthInterceptor>(x => new SubscriptionAuthInterceptor(x.GetApplicationService<TokenValidationParameters>(), x.GetApplicationService<GeexJwtSecurityTokenHandler>(), x.GetApplicationService<IAuthenticationSchemeProvider>()));
                SchemaBuilder.AddSocketSessionInterceptor(x => new SubscriptionAuthInterceptor(x.GetApplicationService<TokenValidationParameters>(), x.GetApplicationService<GeexJwtSecurityTokenHandler>(), x.GetApplicationService<IAuthenticationSchemeProvider>()));

                services.AddSingleton<IMongoDatabase>(DB.DefaultDb);
                services.AddOpenIddict()
                    .AddCore(options =>
                    {
                        options.UseMongoDb();
                    })
                    .AddServer(options =>
                    {
                        options.AllowRefreshTokenFlow();
                        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(authOptions.TokenExpireInSeconds));

                        // Enable the authorization and token endpoints.
                        options
                            //connect/checksession
                            .SetUserinfoEndpointUris("/idsvr/userinfo")
                            .SetLogoutEndpointUris("/idsvr/endsession")
                            .SetRevocationEndpointUris("/idsvr/revocation")
                            .SetCryptographyEndpointUris("/.well-known/openid-configuration/jwks")
                            .SetIntrospectionEndpointUris("/idsvr/introspect")
                            .SetVerificationEndpointUris("/idsvr/deviceauthorization")
                            .SetAuthorizationEndpointUris("/idsvr/authorize")
                            .SetDeviceEndpointUris("/idsvr/device")
                            .SetTokenEndpointUris("/idsvr/token");

                        options.RegisterClaims(
                            GeexClaimType.Provider,
                            GeexClaimType.Sub,
                            GeexClaimType.Tenant,
                            GeexClaimType.Role,
                            GeexClaimType.Org,
                            GeexClaimType.ClientId,
                            GeexClaimType.Expires,
                            GeexClaimType.FullName,
                            GeexClaimType.Nickname
                        );

                        options.RegisterScopes(
                            OpenIddictConstants.Scopes.OpenId,
                            OpenIddictConstants.Scopes.Email,
                            OpenIddictConstants.Scopes.Phone,
                            OpenIddictConstants.Scopes.Profile,
                            OpenIddictConstants.Scopes.Roles,
                            OpenIddictConstants.Scopes.OfflineAccess
                            );

                        // Enable the flows.
                        options.AllowAuthorizationCodeFlow()
                            .RequireProofKeyForCodeExchange()
                            .AllowClientCredentialsFlow()
                            .AllowDeviceCodeFlow()
                            .AllowRefreshTokenFlow()
                            .AllowImplicitFlow()
                            ;

                        // Register the signing and encryption credentials.
                        if (Env.IsDevelopment())
                        {
                            options.AddDevelopmentEncryptionCertificate()
                              .AddDevelopmentSigningCertificate();
                        }
                        else
                        {
                            // todo:
                            //options.AddEncryptionCertificate().AddSigningCertificate()
                            options.AddDevelopmentEncryptionCertificate()
                              .AddDevelopmentSigningCertificate();
                        }


                        options.DisableAccessTokenEncryption();

                        // 配置选项
                        options.Configure(x =>
                        {
                            x.IgnoreEndpointPermissions = true;
                            x.IgnoreResponseTypePermissions = true;
                        });

                        // Register the ASP.NET Core host and configure the authorization endpoint
                        // to allow the /authorize minimal API handler to handle authorization requests
                        // after being validated by the built-in OpenIddict server event handlers.
                        //
                        // Token requests will be handled by OpenIddict itself by reusing the identity
                        // created by the /authorize handler and stored in the authorization codes.
                        var aspNetCoreBuilder = options.UseAspNetCore();
                        aspNetCoreBuilder
                            .EnableAuthorizationEndpointPassthrough()
                             .EnableLogoutEndpointPassthrough()
                             .EnableTokenEndpointPassthrough()
                             .EnableUserinfoEndpointPassthrough()
                             .EnableStatusCodePagesIntegration();

                        if (Env.IsDevelopment())
                        {
                            aspNetCoreBuilder.DisableTransportSecurityRequirement();

                        }
                    })
                    .AddValidation(options =>
                    {
                        // Import the configuration from the local OpenIddict server instance.
                        options.UseLocalServer();
                        // Register the ASP.NET Core host.
                        options.UseAspNetCore();
                    });

            }
            else
            {
                authenticationBuilder.AddScheme<AuthenticationSchemeOptions, SuperAdminAuthHandler>("SuperAdmin", "SuperAdmin", (
                    o =>
                    {
                        o.ForwardDefaultSelector = httpContext =>
                        {
                            var schema = httpContext.Request.Headers.Authorization.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                            return schema;
                        };
                    }));
            }
            base.ConfigureServices(context);
        }

        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseAuthentication();
            //app.UseMiddleware<IdsvrMiddleware>();
            app.Map("/idsvr", x => x.UseMiddleware<IdsvrMiddleware>());
            app.UseAuthorization();
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
