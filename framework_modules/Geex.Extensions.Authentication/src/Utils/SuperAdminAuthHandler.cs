using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Geex.Extensions.Authentication.Domain;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geex.Extensions.Authentication.Utils
{
    public class SuperAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationHandler
    {
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;

        private const string AuthorizationHeaderName = "Authorization";
        public const string SchemeName = "SuperAdmin";
        public static string? AdminToken;

        /// <inheritdoc />
        public SuperAdminAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<AuthenticationSchemeOptions> options,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions tokenGenerateOptions,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock) : base(options,
            logger, encoder, clock)
        {
            _tokenHandler = tokenHandler;
            if (AdminToken.IsNullOrEmpty())
            {
                var staticTokenGenerationOptions = new UserTokenGenerateOptions(tokenGenerateOptions.Issuer, tokenGenerateOptions.Audience, tokenGenerateOptions.SigningCredentials, null);
                var token = _tokenHandler.CreateJwtSecurityToken(new GeexSecurityTokenDescriptor(GeexConstants.SuperAdminId, LoginProviderEnum.Local, staticTokenGenerationOptions));
                AdminToken = token.UnsafeToString();
            }
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

            if (userAndPassword != AdminToken)
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
