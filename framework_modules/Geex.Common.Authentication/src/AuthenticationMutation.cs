using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Authentication.Domain;
using Geex.Common.Requests.Authentication;

using HotChocolate;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Authentication
{
    public sealed class AuthenticationMutation : MutationExtension<AuthenticationMutation>
    {

        private readonly IUnitOfWork _uow;

        public AuthenticationMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<UserToken> Authenticate(AuthenticateRequest request) => await _uow.Request(request);

        public async Task<UserToken> FederateAuthenticate(FederateAuthenticateRequest request) => await _uow.Request(request);

        public async Task<bool> CancelAuthentication(
            [Service] ICurrentUser currentUser
            )
        {
            var userId = currentUser?.UserId;
            if (!userId.IsNullOrEmpty())
            {
                return await _uow.Request(new CancelAuthenticationRequest(userId));
            }
            return false;
        }
    }
}
