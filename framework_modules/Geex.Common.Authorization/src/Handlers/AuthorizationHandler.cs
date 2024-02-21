using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Events;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Authorization.Casbin;
using Geex.Common.Authorization.Events;
using Geex.Common.Authorization.GqlSchema.Inputs;

using MediatR;

using NetCasbin;

using static Geex.Common.Abstraction.GqlConfig;

namespace Geex.Common.Authorization.Handlers
{
    public class AuthorizationHandler : IRequestHandler<UserRoleChangeRequest>,
        IRequestHandler<GetSubjectPermissionsRequest, IEnumerable<string>>,
        IRequestHandler<AuthorizeInput>
    {
        private IMediator _mediator;

        public AuthorizationHandler(IRbacEnforcer enforcer, IMediator mediator)
        {
            _enforcer = enforcer;
            _mediator = mediator;
        }

        private IRbacEnforcer _enforcer { get; init; }
        public async Task Handle(UserRoleChangeRequest notification, CancellationToken cancellationToken)
        {
            await _enforcer.SetRoles(notification.UserId, notification.RoleIds);
            return;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IEnumerable<string>> Handle(GetSubjectPermissionsRequest request, CancellationToken cancellationToken)
        {
            return _enforcer.GetImplicitPermissionsForUser(request.Subject);
        }

        public async Task Handle(AuthorizeInput request, CancellationToken cancellationToken)
        {
            var permissions = request.AllowedPermissions.Select(x => x.Value);
            await _enforcer.SetPermissionsAsync(request.Target, permissions);
            await _mediator.Publish(new PermissionChangedEvent(request.Target, permissions.ToArray()), cancellationToken);
            return;
        }
    }
}
