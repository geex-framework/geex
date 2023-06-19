using System.Threading.Tasks;

using Autofac;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs;

using HotChocolate;
using HotChocolate.Types;
using MediatR;

using MongoDB.Entities;

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
            CreateRoleInput input)
        {
            return await _mediator.Send(input);
        }

         public async Task<bool> SetRoleDefault(
            SetRoleDefaultInput input)
        {
            await _mediator.Send(input);
            return true;
        }
    }
}
