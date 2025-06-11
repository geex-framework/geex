using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions;
using Geex.Extensions.Authentication.Core.Utils;

using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;
using MongoDB.Entities;

using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;


namespace Geex.Extensions.Authentication
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class AuthenticationModule : GeexModule<AuthenticationModule, AuthenticationModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            IdentityModelEventSource.ShowPII = true;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var services = context.Services;
            services.AddTransient<IPasswordHasher<IAuthUser>, PasswordHasher<IAuthUser>>();
            var geexCoreModuleOptions = services.GetSingletonInstance<GeexCoreModuleOptions>();
            var moduleOptions = services.GetSingletonInstance<AuthenticationModuleOptions>();

            services.AddSingleton<GeexJwtSecurityTokenHandler>();
            var authenticationBuilder = services.AddAuthentication(AuthSchemeRoutingHandler.SchemeName);

            if (moduleOptions != default)
            {
                X509Certificate2? cert2 = default;
                SecurityKey? securityKey = default;
                SigningCredentials? signCredentials = default;
                var certFile = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
                var certPassword = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password");
                if (!string.IsNullOrEmpty(certFile))
                {
                    // Get the linked file name from the path
                    var certFileName = Path.GetFileName(certFile);

                    // Try multiple possible locations
                    var possiblePaths = new[]
                    {
                        // Direct path if fully qualified
                        Path.IsPathFullyQualified(certFile) ? certFile : null,
                        // Project directory path
                        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, certFile)),
                        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../", certFile)),
                        // Bin directory path
                        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, certFile)),
                        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../../../", certFile)),
                    };
                    // Use first existing file path
                    certFile = possiblePaths.FirstOrDefault(p => p != null && Path.IsPathFullyQualified(p) && File.Exists(p));

                    // Register the signing and encryption credentials.
                    if (!string.IsNullOrEmpty(certFile))
                    {
                        cert2 = string.IsNullOrEmpty(certPassword) ? X509CertificateLoader.LoadPkcs12FromFile(certFile, null, keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet) : X509CertificateLoader.LoadPkcs12FromFile(certFile, certPassword, X509KeyStorageFlags.EphemeralKeySet);
                        securityKey = new X509SecurityKey(cert2);
                        signCredentials = new X509SigningCredentials(cert2);
                        services.AddSingleton(cert2);
                    }
                }

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
                        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds));
                        // Enable the authorization and token endpoints.
                        options
                            //connect/checksession
                            .SetUserinfoEndpointUris("/idsvr/userinfo")
                            .SetLogoutEndpointUris("/idsvr/logout")
                            .SetRevocationEndpointUris("/idsvr/revocation")
                            .SetCryptographyEndpointUris("/.well-known/openid-configuration/jwks")
                            .SetIntrospectionEndpointUris("/idsvr/introspect")
                            .SetVerificationEndpointUris("/idsvr/deviceauthorization")
                            .SetAuthorizationEndpointUris("/idsvr/authorize")
                            .SetDeviceEndpointUris("/idsvr/device")
                            .SetTokenEndpointUris("/idsvr/token")
                            .SetConfigurationEndpointUris("/.well-known/openid-configuration");

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
                        options
                            .AllowAuthorizationCodeFlow()
                            .RequireProofKeyForCodeExchange()
                            .AllowClientCredentialsFlow()
                            .AllowDeviceCodeFlow()
                            .AllowRefreshTokenFlow()
                            .AllowImplicitFlow()
                            .AllowHybridFlow()
                            .AllowNoneFlow()
                            .AllowPasswordFlow()
                            ;

                        Console.WriteLine($"Using cert file: {certFile}");

                        // Register the signing and encryption credentials.
                        if (cert2 != default)
                        {
                            options
                                .AddEncryptionCertificate(cert2)
                                .AddSigningCertificate(cert2);
                        }
                        else
                        {
                            var tempCertName = new X500DistinguishedName("CN=OpenIddict Server Encryption Certificate");
                            options
                                .AddDevelopmentEncryptionCertificate(tempCertName)
                                .AddDevelopmentSigningCertificate(tempCertName);
                            using var x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                            x509Store.Open(OpenFlags.ReadOnly);
                            var tempCert = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, (object)tempCertName.Name, false).OfType<X509Certificate2>().FirstOrDefault(x => x.NotBefore < DateTime.Now && x.NotAfter > DateTime.Now);
                            cert2 = tempCert;
                            securityKey = new X509SecurityKey(cert2);
                            signCredentials = new X509SigningCredentials(cert2);
                            services.AddSingleton(cert2);
                            services.AddSingleton<X509Certificate>(cert2);
                        }

                        options.DisableAccessTokenEncryption();

                        // 配置选项
                        options.Configure(x =>
                        {
                            ConfigTokenValidationParameters(x.TokenValidationParameters, cert2, securityKey, moduleOptions);
                            x.IgnoreEndpointPermissions = true;
                            x.IgnoreResponseTypePermissions = true;
                            x.IgnoreGrantTypePermissions = true;
                            x.IgnoreScopePermissions = true;
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
                            .EnableVerificationEndpointPassthrough()
                            .EnableErrorPassthrough()
                            //.EnableStatusCodePagesIntegration()
                            ;

                        aspNetCoreBuilder.DisableTransportSecurityRequirement();
                    })
                    .AddValidation(options =>
                    {
                        // Import the configuration from the local OpenIddict server instance.
                        options.UseLocalServer();
                        // Register the ASP.NET Core host.
                        options.UseAspNetCore();
                    });
                services.AddScoped<ICurrentUser, CurrentUser>();
                services.AddScoped<IClaimsTransformation, GeexClaimsTransformation>();

                var tokenValidationParameters = new TokenValidationParameters();
                ConfigTokenValidationParameters(tokenValidationParameters, cert2, securityKey, moduleOptions);
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
                    .AddScheme<AuthenticationSchemeOptions, AuthSchemeRoutingHandler>(AuthSchemeRoutingHandler.SchemeName, AuthSchemeRoutingHandler.SchemeName, x => { })
                    .AddScheme<JwtBearerOptions, LocalAuthHandler>(LocalAuthHandler.SchemeName, LocalAuthHandler.SchemeName, ConfigJwtBearerOptions)
                    .AddScheme<AuthenticationSchemeOptions, SuperAdminAuthHandler>(SuperAdminAuthHandler.SchemeName, SuperAdminAuthHandler.SchemeName, x =>
                    {
                    })
                    .AddJwtBearer()
                    .AddCookie()
                    ;

                services.AddSingleton(new UserTokenGenerateOptions(cert2?.Issuer, moduleOptions.ValidAudience, signCredentials, TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds)));

            }
            base.ConfigureServices(context);

            void ConfigTokenValidationParameters(TokenValidationParameters x, X509Certificate2? cert, SecurityKey? securityKey,
                AuthenticationModuleOptions authOptions)
            {
                x.RequireSignedTokens = x.ValidateIssuerSigningKey = securityKey != default;
                x.IssuerSigningKey = securityKey;

                x.ValidateIssuer = x.ValidateIssuerSigningKey = !string.IsNullOrEmpty(cert?.Issuer);
                if (!string.IsNullOrEmpty(cert?.Issuer))
                {
                    x.ValidIssuers = [cert.Issuer, new Uri(geexCoreModuleOptions.Host).ToString()];
                }

                x.ValidateAudience = !authOptions.ValidAudience.IsNullOrEmpty();
                x.ValidAudience = authOptions.ValidAudience;

                x.ValidateLifetime = authOptions.TokenExpireInSeconds > 0;
            }
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            //app.UseMiddleware<AuthSchemeRoutingMiddleware>();
            app.UseAuthentication();
            app.Map("/idsvr", x => x.UseMiddleware<IdsvrMiddleware>());
            app.UseAuthorization();
            base.OnPreApplicationInitialization(context);
        }

        /// <inheritdoc />
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            // init admin token and print it to console
            _ = context.ServiceProvider.GetService<SuperAdminAuthHandler>();
            base.OnPostApplicationInitialization(context);
        }
    }
}
