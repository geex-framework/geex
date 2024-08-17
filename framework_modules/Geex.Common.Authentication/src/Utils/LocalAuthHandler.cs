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

namespace Geex.Common.Authentication.Utils
{
    internal class LocalAuthHandler : JwtBearerHandler, IAuthenticationHandler
    {
        private readonly IMediator _mediator;
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private UserTokenGenerateOptions _userTokenGenerateOptions;

        public const string SchemeName = "Local";


        /// <inheritdoc />
        public LocalAuthHandler([NotNull][ItemNotNull] IOptionsMonitor<JwtBearerOptions> options,
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
            var accessToken = request != default ? request.AccessToken : Context.Request.Headers.Authorization.ToString().Split(' ',StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

            if (accessToken.IsNullOrEmpty())
            {
                return AuthenticateResult.NoResult();
            }

            var jwt = _tokenHandler.ReadJwtToken(accessToken);
            var identity = new ClaimsIdentity(jwt.Claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }


    }
}
