using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.Requests.Authorization;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Authorization
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
            descriptor.Field(x=>x.Authorize(default)).Authorize("123");
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
