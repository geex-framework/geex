using System.Threading.Tasks;

using Geex.Abstractions.Gql.Types;
using Geex.Common.Requests.Authorization;

using HotChocolate.Types;

namespace Geex.Common.Authorization
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
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        public async Task<bool> Authorize(
            AuthorizeRequest request)
        {
            await _uow.Request(request);
            return true;
        }
    }
}
