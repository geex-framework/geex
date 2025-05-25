using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Authorization;
using Geex.Entities;
using Geex.Events;
using Geex.Extensions.Authorization.Events;
using Geex.Extensions.Requests.Authorization;
using MediatR;

namespace Geex.Extensions.Authorization.Handlers
{
    public class AuthorizationHandler : IRequestHandler<UserRoleChangeRequest>,
        IRequestHandler<GetSubjectPermissionsRequest, IEnumerable<string>>,
        IRequestHandler<AuthorizeRequest>
    {
        private IUnitOfWork _uow;

        public AuthorizationHandler(IRbacEnforcer enforcer, IUnitOfWork uow)
        {
            _enforcer = enforcer;
            _uow = uow;
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
            await _uow.Notify(new PermissionChangedEvent(request.Target, permissions.ToArray()), cancellationToken);
            return;
        }
    }
}
