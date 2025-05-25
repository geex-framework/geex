using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Authentication;
using Geex.Extensions.AuditLogs;
using Geex.Extensions.Authentication.Domain;
using Geex.Extensions.Requests.Authentication;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication
{
    public sealed class AuthenticationMutation : MutationExtension<AuthenticationMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthenticationMutation> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Field(x=>x.Authenticate(default)).Audit();
            descriptor.Field(x=>x.FederateAuthenticate(default)).Audit();
        }

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
