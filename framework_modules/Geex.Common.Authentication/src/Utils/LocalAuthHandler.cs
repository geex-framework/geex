using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Geex.Common.Abstractions;
using Geex.Common.Authentication.Domain;

using JetBrains.Annotations;

using MediatR;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.DataProtection;

namespace Geex.Common.Authentication.Utils
{
    internal class LocalAuthHandler : JwtBearerHandler, IAuthenticationHandler
    {
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
        public new Task<AuthenticateResult> AuthenticateAsync()
        {
            return this.HandleAuthenticateAsync();
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authResult = AuthenticateResult.NoResult();
            var request = Context.GetOpenIddictServerRequest();
            var parts = Context.Request.Headers.Authorization.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var accessToken = request != default ? request.AccessToken : parts.ElementAtOrDefault(1);
            var schema = parts.ElementAtOrDefault(0);
            try
            {
                if (schema == "SuperAdmin")
                {
                    this.Logger.LogWarning("SuperAdmin is not supported in this version of the API");
                    return authResult;
                }
                if (accessToken.IsNullOrEmpty())
                {
                    this.Logger.LogWarning("Access token is null or empty");
                    return authResult;
                }
                var token = _tokenHandler.ReadToken(accessToken);

                if (token is not null && token.Issuer is not null && token.Issuer.Contains("account.api"))
                {
                    return authResult;
                }

                var result = await _tokenHandler.ValidateTokenAsync(accessToken, _tokenValidationParameters);
                if (result == null || !result.IsValid || result.ClaimsIdentity == null)
                {
                    this.Logger.LogWarningWithData("Access token is invalid or not issued by this API", result?.Exception);
                    authResult = AuthenticateResult.Fail(result.Exception);
                }
                var identity = result.ClaimsIdentity;
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, SchemeName);
                authResult = AuthenticateResult.Success(ticket);
                return authResult;
            }
            catch(Exception ex)
            {
                this.Logger.LogWarningWithData("Access token is invalid or not issued by this API", ex);
                return authResult;
            }
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
