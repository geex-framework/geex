using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Events;
using Geex.Common.Authorization.Events;
using Geex.Common.Requests.Authorization;
using MediatR;

namespace Geex.Common.Authorization.Handlers
{
    public class AuthorizationHandler : IRequestHandler<UserRoleChangeRequest>,
        IRequestHandler<GetSubjectPermissionsRequest, IEnumerable<string>>,
        IRequestHandler<AuthorizeRequest>
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
            if (request.Subject == IUser.SuperAdminId)
            {
                return AppPermission.DynamicValues.Select(x=>x.Value);
            }
            return _enforcer.GetImplicitPermissionsForUser(request.Subject);
        }

        public async Task Handle(AuthorizeRequest request, CancellationToken cancellationToken)
        {
            var permissions = request.AllowedPermissions.Select(x => x.Value);
            await _enforcer.SetPermissionsAsync(request.Target, permissions);
            await _mediator.Publish(new PermissionChangedEvent(request.Target, permissions.ToArray()), cancellationToken);
            return;
        }
    }
}
