using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Geex.Extensions.Authorization.Gql
{
    public sealed class AuthorizationQuery : QueryExtension<AuthorizationQuery>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IMediator _mediator;

        public AuthorizationQuery(ICurrentUser currentUser,IMediator mediator)
        {
            _currentUser = currentUser;
            _mediator = mediator;
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthorizationQuery> descriptor)
        {
            base.Configure(descriptor);
        }

        public async Task<List<string>> MyPermissions(
            [Service] IRbacEnforcer enforcer)
        {
            var myId = _currentUser?.UserId;
            return _mediator.Send(new GetSubjectPermissionsRequest(myId)).Result.ToList();
        }
    }
}
