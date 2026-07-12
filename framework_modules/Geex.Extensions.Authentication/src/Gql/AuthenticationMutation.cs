using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication.Gql
{
    public sealed class AuthenticationMutation : MutationExtension<AuthenticationMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthenticationMutation> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Field(x => x.Authenticate(default));
            descriptor.Field(x => x.FederateAuthenticate(default));
            descriptor.Field(x => x.CancelAuthentication());
            descriptor.Field(x => x.GeneratePersonalAccessToken(default)).Authorize();
        }

        private readonly IUnitOfWork _uow;

        public AuthenticationMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<UserSession> Authenticate(AuthenticateRequest request) => await _uow.Request(request);

        public async Task<UserSession> FederateAuthenticate(FederateAuthenticateRequest request) => await _uow.Request(request);

        public async Task<bool> CancelAuthentication()
        {
            var session = _uow.GetCurrentUser()?.Session;
            if (session == null)
            {
                return false;
            }

            return await session.InvalidateCacheAsync();
        }

        public async Task<UserSession> GeneratePersonalAccessToken(GeneratePersonalAccessTokenRequest request) =>
            await _uow.Request(request);
    }
}
