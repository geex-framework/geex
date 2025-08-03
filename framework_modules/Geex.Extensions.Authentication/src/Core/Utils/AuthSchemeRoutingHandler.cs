using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class AuthSchemeRoutingHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AuthSchemeRouting";
        private const string AuthorizationHeaderName = "Authorization";

        public AuthSchemeRoutingHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.Request;

            // Check for OpenIddict server authentication for idsvr endpoints
            if (request.Path.StartsWithSegments("/idsvr"))
            {
                // For checksession, try cookie authentication first, then validation scheme
                var cookieResult = await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (cookieResult.Succeeded)
                {
                    return cookieResult;
                }

                // Try OpenIddict validation for token-based authentication
                var validationResult = await Context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                if (validationResult.Succeeded)
                {
                    return validationResult;
                }

                return AuthenticateResult.NoResult();
            }

            // Check for Authorization header first
            if (request.Headers.ContainsKey(AuthorizationHeaderName))
            {
                var header = request.Headers[AuthorizationHeaderName];
                if (AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue headerValue))
                {
                    var scheme = headerValue.Scheme?.ToLowerInvariant();

                    switch (scheme)
                    {
                        case "local":
                            return await Context.AuthenticateAsync(LocalAuthHandler.SchemeName);
                        case "superadmin":
                            return await Context.AuthenticateAsync(SuperAdminAuthHandler.SchemeName);
                        case "bearer":
                            return await Context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                    }
                }
            }

            // No authentication found
            return AuthenticateResult.NoResult();
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            return Context.ChallengeAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, properties);
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            return Context.ForbidAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, properties);
        }
    }
}
