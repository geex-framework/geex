using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.GqlSchemas.Users.Inputs;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Users
{
    public class UserMutation : MutationExtension<UserMutation>
    {
        private readonly IMediator _mediator;

        public UserMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        public async Task<bool> AssignRoles(AssignRoleRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        public async Task<bool> AssignOrgs(AssignOrgRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        public async Task<bool> EditUser(EditUserRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }
        public async Task<bool> CreateUser(CreateUserRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        public async Task<bool> ResetUserPassword(ResetUserPasswordRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }
    }
}