using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Requests.Identity;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles
{
    public class RoleMutation : MutationExtension<RoleMutation>
    {
        private readonly IMediator _mediator;

        public RoleMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<RoleMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        public async Task<Role> CreateRole(
            CreateRoleRequest request)
        {
            return await _mediator.Send(request);
        }

         public async Task<bool> SetRoleDefault(
            SetRoleDefaultRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
    }
}
