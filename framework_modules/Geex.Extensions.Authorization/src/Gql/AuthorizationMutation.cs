using System.Threading.Tasks;
using Geex.Extensions.Authorization.Requests;
using Geex.Extensions.Authentication;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Authorization.Gql
{
    public sealed class AuthorizationMutation : MutationExtension<AuthorizationMutation>
    {
        private IUnitOfWork _uow;

        public AuthorizationMutation(IUnitOfWork uow)
        {
            _uow = uow;
        }
        protected override void Configure(IObjectTypeDescriptor<AuthorizationMutation> descriptor)
        {
            descriptor.Field(x=>x.Authorize(default)).Authorize(AuthorizationPermission.Authorize);
            descriptor.Field(x=>x.GeneratePersonalAccessToken(default)).Authorize();
            base.Configure(descriptor);
        }

        public async Task<bool> Authorize(
            AuthorizeRequest request)
        {
            await _uow.Request(request);
            return true;
        }

        public async Task<UserToken> GeneratePersonalAccessToken(GeneratePersonalAccessTokenRequest request)
        {
            return await _uow.Request(request);
        }
    }
}
