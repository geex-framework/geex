using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geex.Extensions.Authentication.Utils
{
    public class SuperAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        private const string AuthorizationHeaderName = "Authorization";
        public const string SchemeName = "SuperAdmin";


        /// <inheritdoc />
        public SuperAdminAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<AuthenticationSchemeOptions> options,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock) : base(options,
            logger, encoder, clock)
        {

        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.Request;
            if (!request.Headers.ContainsKey(AuthorizationHeaderName))
            {
                //Authorization header not in request
                return AuthenticateResult.NoResult();
            }

            var header = request.Headers[AuthorizationHeaderName];
            if (!AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue headerValue))
            {
                //Invalid Authorization header
                return AuthenticateResult.NoResult();
            }

            if (!SchemeName.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                //Not SuperAdmin authentication header
                if (!headerValue.Scheme.IsNullOrEmpty())
                {
                    Options.ForwardDefault ??= headerValue.Scheme;
                }
                return AuthenticateResult.NoResult();
            }

            string userAndPassword = headerValue.Parameter;

            if (userAndPassword.ToLowerInvariant() != "superAdmin".ToMd5().ToLowerInvariant())
            {
                return AuthenticateResult.Fail("Invalid auth parameter.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, GeexConstants.SuperAdminId),
                new Claim(GeexClaimType.FullName, GeexConstants.SuperAdminName),
                new Claim(GeexClaimType.Sub, GeexConstants.SuperAdminId),
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }
    }
}
