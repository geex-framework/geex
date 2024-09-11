using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Requests;
using Geex.Common.Requests.Identity;
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

        public virtual async Task<bool> AssignRoles(AssignRoleRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        public virtual async Task<bool> AssignOrgs(AssignOrgRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        public virtual async Task<bool> EditUser(EditUserRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
        public virtual async Task<bool> CreateUser(CreateUserRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        public virtual async Task<bool> DeleteUser(DeleteUserRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        public virtual async Task<bool> ResetUserPassword(ResetUserPasswordRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
    }
}