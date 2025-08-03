using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Core.Utils
{
    internal class LocalAuthHandler : JwtBearerHandler, IAuthenticationHandler
    {
        private const string AuthorizationHeaderName = "Authorization";
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public const string SchemeName = "Local";


        /// <inheritdoc />
        public LocalAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<JwtBearerOptions> options,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock, GeexJwtSecurityTokenHandler tokenHandler, TokenValidationParameters tokenValidationParameters) : base(options,
            logger, encoder, clock)
        {
            _tokenHandler = tokenHandler;
            _tokenValidationParameters = tokenValidationParameters;
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.Request;
            var openIddictRequest = Context.GetOpenIddictServerRequest();

            // Try to get access token from OpenIddict first, then from Authorization header
            var accessToken = openIddictRequest?.AccessToken;

            if (accessToken.IsNullOrEmpty())
            {
                // Fall back to Authorization header
                if (!request.Headers.TryGetValue(AuthorizationHeaderName, out var header) ||
                    !AuthenticationHeaderValue.TryParse(header, out var headerValue) ||
                    !SchemeName.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    return AuthenticateResult.NoResult();
                }

                accessToken = headerValue.Parameter;
            }

            if (accessToken.IsNullOrEmpty())
            {
                return AuthenticateResult.NoResult();
            }

            var result = await _tokenHandler.ValidateTokenAsync(accessToken, _tokenValidationParameters);
            if (!result.IsValid || result.ClaimsIdentity == null)
            {
                return AuthenticateResult.Fail(result.Exception);
            }
            var principal = new ClaimsPrincipal(result.ClaimsIdentity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }

        /// <inheritdoc />
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            return base.HandleChallengeAsync(properties);
        }

        /// <inheritdoc />
        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            return base.HandleForbiddenAsync(properties);
        }
    }
}
