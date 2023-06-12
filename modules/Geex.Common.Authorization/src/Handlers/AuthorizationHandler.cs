using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Events;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Authorization.Casbin;

using MediatR;

namespace Geex.Common.Authorization.Handlers
{
    public class AuthorizationHandler : IRequestHandler<UserRoleChangeRequest>,
        IRequestHandler<GetSubjectPermissionsRequest, IEnumerable<string>>
    {

        public AuthorizationHandler(IRbacEnforcer enforcer)
        {
            Enforcer = enforcer;
        }

        public IRbacEnforcer Enforcer { get; init; }
        public async Task<Unit> Handle(UserRoleChangeRequest notification, CancellationToken cancellationToken)
        {
            await Enforcer.SetRoles(notification.UserId, notification.RoleIds);
            return Unit.Value;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IEnumerable<string>> Handle(GetSubjectPermissionsRequest request, CancellationToken cancellationToken)
        {
            return Enforcer.GetImplicitPermissionsForUser(request.Subject);
        }
    }
}
