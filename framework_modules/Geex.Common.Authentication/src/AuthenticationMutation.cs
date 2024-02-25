using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Authentication.Domain;
using Geex.Common.Requests.Authentication;
using HotChocolate;

using MediatR;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Authentication
{
    public class AuthenticationMutation : MutationExtension<AuthenticationMutation>
    {
        private readonly IMediator _mediator;

        public AuthenticationMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        public async Task<UserToken> Authenticate(AuthenticateRequest request)
        {
            return await _mediator.Send(request);
        }

        public async Task<UserToken> FederateAuthenticate(FederateAuthenticateRequest request)
        {
            return await _mediator.Send(request);
        }

        public async Task<bool> CancelAuthentication(
            [Service] IRedisDatabase redis,
            [Service] ClaimsPrincipal claimsPrincipal
            )
        {
            var userId = claimsPrincipal?.FindUserId();
            return await _mediator.Send(new CancelAuthenticationRequest(userId));
        }
    }
}
