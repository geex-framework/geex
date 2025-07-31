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
using Microsoft.Extensions.Logging;
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
                var cert = LoadCertificate();

                var (configuredCert, signingCredentials) = ConfigureOpenIddict(services, moduleOptions, geexCoreModuleOptions, cert);
                ConfigureAuthentication(services, authenticationBuilder, configuredCert ?? cert, moduleOptions, geexCoreModuleOptions);
            }

            base.ConfigureServices(context);
        }

        private X509Certificate2 LoadCertificate()
        {
            try
            {
                var certFile = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Endpoints__Https__Certificate__Path");
                var keyPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Endpoints__Https__Certificate__KeyPath");

                if (string.IsNullOrEmpty(certFile))
                {
                    Logger?.LogInformation("未找到证书路径环境变量，将使用开发证书");
                    return null;
                }

                var resolvedCertPath = ResolveCertificatePath(certFile);
                if (string.IsNullOrEmpty(resolvedCertPath))
                {
                    Logger?.LogWarning("证书文件未找到: {CertFile}，将使用开发证书", certFile);
                    return null;
                }

                var cert = CreateCertificateFromFiles(resolvedCertPath, keyPath);
                Logger?.LogInformation("成功加载证书: {CertPath}", resolvedCertPath);

                return cert;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "加载证书时发生错误，将使用开发证书");
                return null;
            }
        }

        private string? ResolveCertificatePath(string certFile)
        {
            if (Path.IsPathFullyQualified(certFile) && File.Exists(certFile))
            {
                return certFile;
            }

            var searchPaths = new[]
            {
                AppContext.BaseDirectory,
                Path.Combine(AppContext.BaseDirectory, "../../../"),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../")
            };

            foreach (var basePath in searchPaths)
            {
                var fullPath = Path.GetFullPath(Path.Combine(basePath, certFile));
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private X509Certificate2 CreateCertificateFromFiles(string certFile, string? keyPath)
        {
            try
            {
                return string.IsNullOrEmpty(keyPath)
                    ? X509Certificate2.CreateFromPemFile(certFile)
                    : X509Certificate2.CreateFromPemFile(certFile, keyPath);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "从文件创建证书时发生错误: {CertFile}, {KeyPath}", certFile, keyPath);
                throw;
            }
        }

        private (X509Certificate2? cert, SigningCredentials? signingCredentials) ConfigureOpenIddict(IServiceCollection services, AuthenticationModuleOptions moduleOptions,
            GeexCoreModuleOptions geexCoreModuleOptions, X509Certificate2 certificateInfo)
        {
            services.AddSingleton<IdsvrMiddleware>();
            services.AddSingleton<ISocketSessionInterceptor, SubscriptionAuthInterceptor>(x =>
                new SubscriptionAuthInterceptor(
                    x.GetApplicationService<TokenValidationParameters>(),
                    x.GetApplicationService<GeexJwtSecurityTokenHandler>(),
                    x.GetApplicationService<IAuthenticationSchemeProvider>()));

            SchemaBuilder.AddSocketSessionInterceptor(x =>
                new SubscriptionAuthInterceptor(
                    x.GetApplicationService<TokenValidationParameters>(),
                    x.GetApplicationService<GeexJwtSecurityTokenHandler>(),
                    x.GetApplicationService<IAuthenticationSchemeProvider>()));

            services.AddSingleton<IMongoDatabase>(DB.DefaultDb);

            X509Certificate2? cert = default;
            SigningCredentials? signCredentials = default;
            services.AddOpenIddict()
                .AddCore(options => options.UseMongoDb())
                .AddServer(options =>
                {
                    ConfigureOpenIddictEndpoints(options);
                    ConfigureOpenIddictClaims(options);
                    ConfigureOpenIddictScopes(options);
                    ConfigureOpenIddictFlows(options);

                    options.SetAccessTokenLifetime(TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds));

                    (cert, signCredentials) = ConfigureOpenIddictCertificates(options, certificateInfo);

                    RegisterCertificateServices(services, cert);

                    // 注册SigningCredentials到服务容器
                    if (signCredentials != null)
                    {
                        services.AddSingleton(signCredentials);
                    }

                    options.DisableAccessTokenEncryption();

                    ConfigureOpenIddictOptions(options, cert, moduleOptions, geexCoreModuleOptions);
                    ConfigureOpenIddictAspNetCore(options);
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            return (cert, signCredentials);
        }

        private void ConfigureOpenIddictEndpoints(OpenIddictServerBuilder options)
        {
            options
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
        }

        private void ConfigureOpenIddictClaims(OpenIddictServerBuilder options)
        {
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
        }

        private void ConfigureOpenIddictScopes(OpenIddictServerBuilder options)
        {
            options.RegisterScopes(
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.Phone,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.Roles,
                OpenIddictConstants.Scopes.OfflineAccess
            );
        }

        private void ConfigureOpenIddictFlows(OpenIddictServerBuilder options)
        {
            options
                .AllowAuthorizationCodeFlow()
                .RequireProofKeyForCodeExchange()
                .AllowClientCredentialsFlow()
                .AllowDeviceCodeFlow()
                .AllowRefreshTokenFlow()
                .AllowImplicitFlow()
                .AllowHybridFlow()
                .AllowNoneFlow()
                .AllowPasswordFlow();
        }

        private (X509Certificate2? signingCert, SigningCredentials? signCredentials) ConfigureOpenIddictCertificates(
            OpenIddictServerBuilder options, X509Certificate2 cert)
        {
            var signingCertName = new X500DistinguishedName("CN=Geex OpenIddict Server Signing Certificate");
            var encryptionCertName = new X500DistinguishedName("CN=Geex OpenIddict Server Encryption Certificate");

            if (cert != null)
            {
                return ConfigureFromExistingCertificate(options, cert, signingCertName, encryptionCertName);
            }
            else
            {
                // 如果没有提供证书，创建自签名的签名和加密证书
                Logger?.LogInformation("未提供证书，正在创建自签名证书");

                var rsa = RSA.Create(2048);
                var certRequest = new CertificateRequest(signingCertName, rsa, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                var securityKey = new RsaSecurityKey(rsa);

                certRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));

                var newCert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
                var algorithm = DetermineSigningAlgorithm(securityKey);
                var signCredentials = new SigningCredentials(securityKey, algorithm);

                options.AddEncryptionCertificate(newCert);
                options.AddSigningCredentials(signCredentials);

                Logger?.LogInformation("成功创建自签名证书用于OpenIddict");
                return (newCert, signCredentials);
            }
        }

        private (X509Certificate2? signingCert, SigningCredentials? signCredentials) ConfigureFromExistingCertificate(
            OpenIddictServerBuilder options, X509Certificate2 cert,
            X500DistinguishedName signingCertName, X500DistinguishedName encryptionCertName)
        {
            Logger?.LogInformation("使用现有证书: {CertName} 进行认证授权", cert.Subject);

            var requiredKeyUsage = X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature;
            var keyUsageExtension = cert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
            var supportedAlgorithm = cert.GetRSAPrivateKey() != default;
            // 如果证书同时支持签名和加密，直接使用
            if (keyUsageExtension != null && (keyUsageExtension.KeyUsages & requiredKeyUsage) == requiredKeyUsage && supportedAlgorithm)
            {
                options
                    .AddEncryptionCertificate(cert)
                    .AddSigningCertificate(cert);
                Logger?.LogInformation("证书支持签名和加密，直接使用");
                return (cert, new X509SigningCredentials(cert));
            }
            // 如果证书不满足要求，创建补充的自签名证书
            else
            {
                Logger?.LogWarning("证书 {CertName} 不满足所需的签名/加密要求，正在创建补充的自签名证书", cert.Subject);

                SecurityKey securityKey;
                CertificateRequest certRequest;
                var certNotBefore = cert.NotBefore;
                var certNotAfter = cert.NotAfter;

                var rsa = cert.GetRSAPrivateKey() ?? RSA.Create(2048);
                certRequest = new CertificateRequest(signingCertName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                securityKey = new RsaSecurityKey(rsa);

                try
                {
                    certRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                    certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));

                    var newCert = certRequest.CreateSelfSigned(certNotBefore, certNotAfter);

                    var algorithm = DetermineSigningAlgorithm(securityKey);
                    var signCredentials = new SigningCredentials(securityKey, algorithm);

                    options.AddSigningCredentials(signCredentials).AddEncryptionCertificate(newCert);
                    Logger?.LogInformation("已添加自签名证书");

                    return (newCert, signCredentials);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "配置自定义证书时发生错误");
                    throw;
                }
            }
        }

        private string DetermineSigningAlgorithm(SecurityKey securityKey)
        {
            var supportedAlgorithms = new[] { "RS256", "HS256", "ES256", "ES384", "ES512" };

            foreach (var algorithm in supportedAlgorithms)
            {
                if (securityKey.IsSupportedAlgorithm(algorithm))
                {
                    return algorithm;
                }
            }

            throw new InvalidOperationException("无法找到支持的签名算法");
        }

        private void RegisterCertificateServices(IServiceCollection services, X509Certificate2? cert)
        {
            if (cert != null)
            {
                services.AddSingleton(cert);
                services.AddSingleton<X509Certificate>(cert);
            }
        }

        private void ConfigureOpenIddictOptions(OpenIddictServerBuilder options, X509Certificate2? cert,
            AuthenticationModuleOptions moduleOptions, GeexCoreModuleOptions geexCoreModuleOptions)
        {
            options.Configure(x =>
            {
                ConfigTokenValidationParameters(x.TokenValidationParameters, cert, moduleOptions, geexCoreModuleOptions);
                x.IgnoreEndpointPermissions = true;
                x.IgnoreResponseTypePermissions = true;
                x.IgnoreGrantTypePermissions = true;
                x.IgnoreScopePermissions = true;
            });
        }

        private void ConfigureOpenIddictAspNetCore(OpenIddictServerBuilder options)
        {
            var aspNetCoreBuilder = options.UseAspNetCore();
            aspNetCoreBuilder
                .EnableAuthorizationEndpointPassthrough()
                .EnableLogoutEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserinfoEndpointPassthrough()
                .EnableVerificationEndpointPassthrough()
                .EnableErrorPassthrough()
                .DisableTransportSecurityRequirement();
        }

        private void ConfigureAuthentication(IServiceCollection services, AuthenticationBuilder authenticationBuilder,
            X509Certificate2 cert, AuthenticationModuleOptions moduleOptions, GeexCoreModuleOptions geexCoreModuleOptions)
        {
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IClaimsTransformation, GeexClaimsTransformation>();

            var tokenValidationParameters = new TokenValidationParameters();
            ConfigTokenValidationParameters(tokenValidationParameters, cert, moduleOptions, geexCoreModuleOptions);
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
                .AddScheme<AuthenticationSchemeOptions, SuperAdminAuthHandler>(SuperAdminAuthHandler.SchemeName, SuperAdminAuthHandler.SchemeName, x => { })
                .AddJwtBearer()
                .AddCookie();

            services.AddSingleton<UserTokenGenerateOptions>(serviceProvider =>
            {
                var signingCredentials = serviceProvider.GetService<SigningCredentials>();
                return new UserTokenGenerateOptions(
                    cert?.Issuer,
                    moduleOptions.ValidAudience,
                    signingCredentials,
                    TimeSpan.FromSeconds(moduleOptions.TokenExpireInSeconds));
            });
        }

        private void ConfigTokenValidationParameters(TokenValidationParameters parameters, X509Certificate2? cert,
            AuthenticationModuleOptions authOptions, GeexCoreModuleOptions geexCoreModuleOptions)
        {
            SecurityKey? securityKey = null;

            if (cert != null)
            {
                securityKey = cert.GetECDsaPrivateKey() is { } ecDsa
                    ? new ECDsaSecurityKey(ecDsa)
                    : new X509SecurityKey(cert);
            }

            parameters.RequireSignedTokens = parameters.ValidateIssuerSigningKey = securityKey != null;
            parameters.IssuerSigningKey = securityKey;

            parameters.ValidateIssuer = !string.IsNullOrEmpty(cert?.Issuer);
            if (!string.IsNullOrEmpty(cert?.Issuer))
            {
                parameters.ValidIssuers = [cert.Issuer, new Uri(geexCoreModuleOptions.Host).ToString()];
            }

            parameters.ValidateAudience = !authOptions.ValidAudience.IsNullOrEmpty();
            parameters.ValidAudience = authOptions.ValidAudience;
            parameters.ValidateLifetime = authOptions.TokenExpireInSeconds > 0;
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseAuthentication();
            app.Map("/idsvr", x => x.UseMiddleware<IdsvrMiddleware>());
            app.UseAuthorization();
            base.OnPreApplicationInitialization(context);
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            _ = context.ServiceProvider.GetService<SuperAdminAuthHandler>();
            base.OnPostApplicationInitialization(context);
        }
    }
}
