using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Autofac;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Authorization.Casbin;
using Geex.Common.Authorization.GqlSchema.Inputs;

using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.Authorization
{
    public class AuthorizationQuery : QueryExtension<AuthorizationQuery>
    {
        private readonly LazyService<ClaimsPrincipal> _claimsPrincipal;
        private readonly IMediator _mediator;

        public AuthorizationQuery(LazyService<ClaimsPrincipal> claimsPrincipal,IMediator mediator)
        {
            _claimsPrincipal = claimsPrincipal;
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
            var myId = _claimsPrincipal?.Value?.Identity?.FindUserId();
            return _mediator.Send(new GetSubjectPermissionsRequest(myId)).Result.ToList();
        }
    }
}
