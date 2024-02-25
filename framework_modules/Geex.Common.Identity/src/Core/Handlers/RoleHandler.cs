using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Requests;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Requests.Identity;
using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Identity.Core.Handlers
{
    public class RoleHandler :
        IRequestHandler<QueryRequest<Role>, IQueryable<Role>>,
        IRequestHandler<CreateRoleRequest, Role>,
        IRequestHandler<SetRoleDefaultRequest>,
        ICommonHandler<IRole, Role>
    {
        public IUnitOfWork Uow { get; }

        public RoleHandler(IUnitOfWork uow)
        {
            Uow = uow;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IQueryable<Role>> Handle(QueryRequest<Role> request, CancellationToken cancellationToken)
        {
            return Uow.Query<Role>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Role> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
        {
            var role = Role.Create(request.RoleCode, request.RoleName, request.IsStatic ?? false, request.IsDefault ?? false);
            Uow.Attach(role);
            await role.SaveAsync(cancellationToken);
            return role;
        }

        /// <inheritdoc />
        public async Task Handle(SetRoleDefaultRequest request, CancellationToken cancellationToken)
        {
            var originDefaultRoles = Uow.Query<Role>().Where(x=>x.IsDefault);
            foreach (var originDefaultRole in originDefaultRoles)
            {
                originDefaultRole.IsDefault = false;
            }
            var role = Uow.Query<Role>().FirstOrDefault(x=>x.Id == request.RoleId);
            role.IsDefault = true;
            return;
        }
    }
}
