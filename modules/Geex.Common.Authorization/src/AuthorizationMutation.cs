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
            [Service] ICurrentTenant currentTenant,
            AuthorizeInput input)
        {
            var permissions = input.AllowedPermissions.Select(x=>x.Value);
            await enforcer.SetPermissionsAsync(input.Target, permissions);
            await mediator.Publish(new PermissionChangedEvent(input.Target, permissions.ToArray()));
            return true;
        }
    }
}
