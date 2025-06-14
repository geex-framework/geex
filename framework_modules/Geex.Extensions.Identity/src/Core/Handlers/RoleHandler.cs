using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;

using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Requests;
using MediatX;

using MongoDB.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Requests;

namespace Geex.Extensions.Identity.Core.Handlers
{
    public class RoleHandler :
        IRequestHandler<QueryRequest<Role>, IQueryable<IRole>>,
        IRequestHandler<CreateRoleRequest, IRole>,
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
        public virtual async Task<IQueryable<IRole>> Handle(QueryRequest<Role> request, CancellationToken cancellationToken)
        {
            return Uow.Query<Role>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IRole> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
        {
            var role = new Role(request.RoleCode, request.RoleName, request.IsStatic ?? false, request.IsDefault ?? false);
            Uow.Attach(role);
            await role.SaveAsync(cancellationToken);
            return role;
        }

        /// <inheritdoc />
        public virtual async Task Handle(SetRoleDefaultRequest request, CancellationToken cancellationToken)
        {
            var originDefaultRoles = Uow.Query<IRole>().Where(x=>x.IsDefault);
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
