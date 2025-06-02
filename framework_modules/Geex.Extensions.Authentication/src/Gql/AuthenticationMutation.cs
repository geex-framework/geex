using System;
using System.Threading.Tasks;
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
        }

        private readonly IUnitOfWork _uow;

        public AuthenticationMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<UserToken> Authenticate(AuthenticateRequest request) => await _uow.Request(request);

        public async Task<UserToken> FederateAuthenticate(FederateAuthenticateRequest request) => await _uow.Request(request);

        public async Task<bool> CancelAuthentication()
        {
            var currentUser = _uow.ServiceProvider.GetService<ICurrentUser>();
            var userId = currentUser?.UserId;
            if (!userId.IsNullOrEmpty())
            {
                return await _uow.Request(new CancelAuthenticationRequest(userId));
            }
            return false;
        }
    }
}
