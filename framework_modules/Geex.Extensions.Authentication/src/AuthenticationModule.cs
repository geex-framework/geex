using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Geex.Extensions.Authentication.Core.Utils;

using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;
using MongoDB.Entities;

using OpenIddict.Abstractions;

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
                X509Certificate2? cert = default;
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
                        cert = string.IsNullOrEmpty(keyPath) ? X509Certificate2.CreateFromPemFile(certFile) : X509Certificate2.CreateFromPemFile(certFile, keyPath);
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

                        // Register the signing and encryption credentials.
                        var x509KeyUsageFlags = (X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature);
                        if ((cert?.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages &
                             x509KeyUsageFlags) == x509KeyUsageFlags)
                        {
                            options
                                .AddEncryptionCertificate(cert)
                                .AddSigningCertificate(cert);
                        }
                        else
                        {
                            SecurityKey securityKey;
                            var x500DistinguishedName = new X500DistinguishedName("CN=Geex OpenIddict Server Signing Certificate");
                            CertificateRequest certRequest;
                            if (cert.GetECDsaPrivateKey() is { } ecDsa)
                            {
                                certRequest = new CertificateRequest(x500DistinguishedName, ecDsa, HashAlgorithmName.SHA256);
                                securityKey = new ECDsaSecurityKey(ecDsa);
                            }
                            else
                            {
                                var rsa = cert.GetRSAPrivateKey() ?? RSA.Create(2048);
                                certRequest = new CertificateRequest(x500DistinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                securityKey = new RsaSecurityKey(rsa);
                            }
                            certRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                            certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));
                            cert = certRequest.CreateSelfSigned(cert.NotBefore, cert.NotAfter);
                            // 导出并重新导入以确保私钥正确关联
                            var password = cert.GetKeyAlgorithmParametersString();
                            byte[] pfxBytes = cert.Export(X509ContentType.Pfx, password);
                            cert = new X509Certificate2(pfxBytes, password,
                                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                            string algorithm;
                            if (securityKey.IsSupportedAlgorithm("RS256"))
                                algorithm = "RS256";
                            else if (securityKey.IsSupportedAlgorithm("HS256"))
                                algorithm = "HS256";
                            else if (securityKey.IsSupportedAlgorithm("ES256"))
                                algorithm = "ES256";
                            else if (securityKey.IsSupportedAlgorithm("ES384"))
                                algorithm = "ES384";
                            else if (securityKey.IsSupportedAlgorithm("ES512"))
                                algorithm = "ES512";
                            else
                                throw new InvalidOperationException(OpenIddictResources.GetResourceString("ID0068"));
                            signCredentials = new SigningCredentials(securityKey, algorithm);
                        }
                        options.AddDevelopmentEncryptionCertificate(new X500DistinguishedName("CN=Geex OpenIddict Server Encryption Certificate"));
                        options.AddSigningCredentials(signCredentials);
                        services.AddSingleton(cert);
                        services.AddSingleton<X509Certificate>(cert);
                        options.DisableAccessTokenEncryption();

                        // 配置选项
                        options.Configure(x =>
                        {
                            ConfigTokenValidationParameters(x.TokenValidationParameters, cert, moduleOptions);
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
                ConfigTokenValidationParameters(tokenValidationParameters, cert, moduleOptions);
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

                services.AddSingleton(new UserTokenGenerateOptions(cert?.Issuer, moduleOptions.ValidAudience, signCredentials, TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds)));

            }
            base.ConfigureServices(context);

            void ConfigTokenValidationParameters(TokenValidationParameters x, X509Certificate2? cert,
                AuthenticationModuleOptions authOptions)
            {
                SecurityKey? securityKey;
                if (cert.GetECDsaPrivateKey() is { } ecDsa)
                {
                    securityKey = new ECDsaSecurityKey(ecDsa);
                }
                else
                {
                    var rsa = cert.GetRSAPrivateKey() ?? RSA.Create(2048);
                    securityKey = new RsaSecurityKey(rsa);
                }
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
