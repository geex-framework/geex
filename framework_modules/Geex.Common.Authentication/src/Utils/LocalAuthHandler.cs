using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Geex.Common.Authentication.Domain;

using JetBrains.Annotations;

using MediatR;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geex.Common.Authentication.Utils
{
    internal class LocalAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IMediator _mediator;
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private UserTokenGenerateOptions _userTokenGenerateOptions;

        public const string SchemeName = "Local";


        /// <inheritdoc />
        public LocalAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<AuthenticationSchemeOptions> options,
            [NotNull] ILoggerFactory logger, [NotNull] UrlEncoder encoder, [NotNull] ISystemClock clock, IMediator mediator, GeexJwtSecurityTokenHandler tokenHandler, UserTokenGenerateOptions userTokenGenerateOptions) : base(options,
            logger, encoder, clock)
        {
            _mediator = mediator;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.GetOpenIddictServerRequest();

            if (request == default)
            {
                return AuthenticateResult.NoResult();
            }

            if (request.AccessToken.IsNullOrEmpty())
            {
                return AuthenticateResult.NoResult();
            }

            var jwt = _tokenHandler.ReadJwtToken(request.AccessToken);
            var identity = new ClaimsIdentity(jwt.Claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }


    }
}
