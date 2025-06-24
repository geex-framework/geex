using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
                X509Certificate2? tlsCert = default;
                X509Certificate2? signingCert = default;
                SecurityKey? securityKey = default;
                SigningCredentials? signCredentials = default;
                var certFile = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Endpoints__Https__Certificate__Path");
                var keyPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Endpoints__Https__Certificate__KeyPath");
                if (!string.IsNullOrEmpty(certFile))
                {
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
                        tlsCert = string.IsNullOrEmpty(keyPath) ? X509Certificate2.CreateFromPemFile(certFile) : X509Certificate2.CreateFromPemFile(certFile, keyPath);
                    }
                    else
                    {
                        Console.WriteLine($"Certificate file not found: {certFile}");
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

                        using var x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        x509Store.Open(OpenFlags.ReadWrite);
                        var signatureUsageFlags = (X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment);
                        if ((tlsCert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages &
                             signatureUsageFlags) == signatureUsageFlags)
                        {
                            signingCert = tlsCert;
                        }
                        else
                        {
                            var certName = new X500DistinguishedName("CN=Geex OpenIddict Server Certificate");
                            signingCert = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, (object)certName.Name, false).OfType<X509Certificate2>().FirstOrDefault(x => x.NotBefore < DateTime.Now && x.NotAfter > DateTime.Now);
                            if (signingCert == default)
                            {
                                // 从 ACME 证书中提取信息
                                var subject = certName;
                                var notBefore = tlsCert.NotBefore;
                                var notAfter = tlsCert.NotAfter;

                                // 创建新的 RSA 密钥对
                                using var rsa = RSA.Create();
                                var keyContent = File.ReadAllText(keyPath);
                                rsa.ImportFromPem(keyContent);
                                // 创建证书请求
                                var request = new CertificateRequest(
                                    subject,
                                    rsa,
                                    HashAlgorithmName.SHA256,
                                    RSASignaturePadding.Pkcs1
                                );

                                // 添加扩展用于 JWT 签名
                                request.CertificateExtensions.Add(
                                    new X509KeyUsageExtension(
                                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                                        critical: true
                                    )
                                );

                                // 添加增强密钥用法
                                request.CertificateExtensions.Add(
                                    new X509EnhancedKeyUsageExtension(
                                        new OidCollection
                                        {
                                            new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                                            new Oid("1.3.6.1.5.5.7.3.2")  // Client Authentication
                                        },
                                        critical: true
                                    )
                                );

                                // 生成自签名证书，使用相同的有效期
                                signingCert = request.CreateSelfSigned(notBefore, notAfter);
                            }
                        }
                        securityKey = new X509SecurityKey(signingCert);
                        signCredentials = new X509SigningCredentials(signingCert);
                        services.AddSingleton(signingCert);
                        services.AddSingleton<X509Certificate>(signingCert);
                        options.AddEncryptionCertificate(signingCert)
                                .AddSigningCertificate(signingCert);
                        options.DisableAccessTokenEncryption();

                        // 配置选项
                        options.Configure(x =>
                        {
                            ConfigTokenValidationParameters(x.TokenValidationParameters, signingCert, securityKey, moduleOptions);
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
                ConfigTokenValidationParameters(tokenValidationParameters, tlsCert, securityKey, moduleOptions);
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

                services.AddSingleton(new UserTokenGenerateOptions(tlsCert?.Issuer, moduleOptions.ValidAudience, signCredentials, TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds)));

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
