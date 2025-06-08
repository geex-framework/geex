using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class SuperAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationHandler
    {
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidationParameters;

        private const string AuthorizationHeaderName = "Authorization";
        public const string SchemeName = "SuperAdmin";
        public static string? AdminTokenStr;
        public static DateTime AdminTokenIssuedAt { get; set; }

        /// <inheritdoc />
        public SuperAdminAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<AuthenticationSchemeOptions> options,
            GeexJwtSecurityTokenHandler tokenHandler,
            TokenValidationParameters tokenValidationParameters,
            UserTokenGenerateOptions tokenGenerateOptions,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock) : base(options,
            logger, encoder, clock)
        {
            _tokenHandler = tokenHandler;
            _tokenValidationParameters = tokenValidationParameters;
            if (AdminTokenStr.IsNullOrEmpty())
            {
                var staticTokenGenerationOptions = new UserTokenGenerateOptions(tokenGenerateOptions.Issuer, tokenGenerateOptions.Audience, tokenGenerateOptions.SigningCredentials, null);
                var token = _tokenHandler.CreateJwtSecurityToken(new GeexSecurityTokenDescriptor(GeexConstants.SuperAdminId, LoginProviderEnum.Local, staticTokenGenerationOptions));
                AdminTokenIssuedAt = token.IssuedAt;
                AdminTokenStr = token.UnsafeToString();
                Console.WriteLine("AdminToken generated:");
                Console.WriteLine($"SuperAdmin {AdminTokenStr}");
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
                return AuthenticateResult.NoResult();
            }

            var result = await _tokenHandler.ValidateTokenAsync(headerValue.Parameter, _tokenValidationParameters);
            if (!result.IsValid || result.SecurityToken.ValidFrom != AdminTokenIssuedAt)
                return AuthenticateResult.NoResult();

            var identity = result.ClaimsIdentity;
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }
    }
}
