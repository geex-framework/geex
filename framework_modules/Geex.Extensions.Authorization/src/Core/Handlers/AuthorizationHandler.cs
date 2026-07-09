using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.Authorization.Core.Casbin;
using Geex.Extensions.Authorization.Events;
using Geex.Extensions.Authorization.Gql.Types;
using Geex.Extensions.Authorization.Requests;
using Geex.Extensions.Authentication;

using MediatX;

namespace Geex.Extensions.Authorization.Core.Handlers
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
            var user = _uow.Query<IAuthUser>().GetById(notification.UserId);
            if (user != null)
            {
                await user.InvalidateSessionsCacheAsync(cancellationToken);
            }
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IEnumerable<string>> Handle(GetSubjectPermissionsRequest request, CancellationToken cancellationToken)
        {
            if (request.Subject == GeexConstants.SuperAdminId)
            {
                return AppPermission.DynamicValues.Select(x => x.Value);
            }
            return _enforcer.GetImplicitPermissionsForUser(request.Subject);
        }

        public async Task Handle(AuthorizeRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Target))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "AuthorizeRequest.Target cannot be null or empty.");
            }
            var permissions = request.AllowedPermissions.Select(x => x.Value);
            await _enforcer.SetPermissionsAsync(request.Target, permissions);
            await _uow.Notify(new PermissionChangedEvent(request.Target, permissions.ToArray()), cancellationToken);
            if (request.AuthorizeTargetType == AuthorizeTargetType.User)
            {
                var user = _uow.Query<IAuthUser>().GetById(request.Target);
                if (user != null)
                {
                    await user.InvalidateSessionsCacheAsync(cancellationToken);
                }
            }
            else if (request.AuthorizeTargetType == AuthorizeTargetType.Role)
            {
                foreach (var userId in _enforcer.GetUsersForRole(request.Target))
                {
                    var user = _uow.Query<IAuthUser>().GetById(userId);
                    if (user != null)
                    {
                        await user.InvalidateSessionsCacheAsync(cancellationToken);
                    }
                }
            }
        }
    }
}
