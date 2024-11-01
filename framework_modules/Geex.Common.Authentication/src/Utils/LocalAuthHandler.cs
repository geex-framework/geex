using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

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
using OpenIddict.Validation.DataProtection;

namespace Geex.Common.Authentication.Utils
{
    internal class LocalAuthHandler : JwtBearerHandler, IAuthenticationHandler
    {
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenManager;

        public const string SchemeName = "Local";


        /// <inheritdoc />
        public LocalAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<JwtBearerOptions> options,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock, GeexJwtSecurityTokenHandler tokenHandler, TokenValidationParameters tokenManager) : base(options,
            logger, encoder, clock)
        {
            _tokenHandler = tokenHandler;
            _tokenManager = tokenManager;
        }


        /// <inheritdoc />
        public new Task<AuthenticateResult> AuthenticateAsync()
        {
            return this.HandleAuthenticateAsync();
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.GetOpenIddictServerRequest();
            var accessToken = request != default ? request.AccessToken : Context.Request.Headers.Authorization.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

            if (accessToken.IsNullOrEmpty())
            {
                return AuthenticateResult.NoResult();
            }
            var result = await _tokenHandler.ValidateTokenAsync(accessToken, _tokenManager);
            var identity = result.ClaimsIdentity;
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }


    }
}
