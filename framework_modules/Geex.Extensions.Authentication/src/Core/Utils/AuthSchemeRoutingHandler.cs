using System;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                            return await Context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                    }
                }
            }

            // Check for authentication cookie as fallback
            if (request.Cookies.Count > 0)
            {
                var cookieResult = await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (cookieResult.Succeeded)
                {
                    return cookieResult;
                }
            }

            // No authentication found
            return AuthenticateResult.NoResult();
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            return Context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, properties);
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            return Context.ForbidAsync(JwtBearerDefaults.AuthenticationScheme, properties);
        }
    }
}
