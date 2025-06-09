using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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
                return AuthenticateResult.NoResult();
            }

            var openIddictRequest = Context.GetOpenIddictServerRequest();

            var accessToken = openIddictRequest != default ? openIddictRequest.AccessToken : headerValue.Parameter;

            if (accessToken.IsNullOrEmpty())
            {
                return AuthenticateResult.NoResult();
            }
            var result = await _tokenHandler.ValidateTokenAsync(accessToken, _tokenValidationParameters);
            var identity = result.ClaimsIdentity;
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
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
