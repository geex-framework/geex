using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Authorization.Casbin;
using Geex.Common.Authorization.Events;
using Geex.Common.Authorization.GqlSchema.Inputs;
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
            AuthorizeInput input)
        {
            await mediator.Send(input);
            return true;
        }
    }
}
