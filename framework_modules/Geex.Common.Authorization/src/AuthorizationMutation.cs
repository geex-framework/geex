using System.Threading.Tasks;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Authorization;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.Authorization
{
    public class AuthorizationMutation : MutationExtension<AuthorizationMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<AuthorizationMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        public async Task<bool> Authorize(
            [Service] IRbacEnforcer enforcer,
            [Service] IMediator mediator,
            AuthorizeRequest request)
        {
            await mediator.Send(request);
            return true;
        }
    }
}
