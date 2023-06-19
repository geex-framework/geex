using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Identity.Core.Handlers
{
    public class RoleHandler :
        IRequestHandler<QueryInput<Role>, IQueryable<Role>>,
        IRequestHandler<CreateRoleInput, Role>,
        IRequestHandler<SetRoleDefaultInput, Unit>,
        ICommonHandler<IRole, Role>
    {
        public DbContext DbContext { get; }

        public RoleHandler(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IQueryable<Role>> Handle(QueryInput<Role> request, CancellationToken cancellationToken)
        {
            return DbContext.Queryable<Role>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Role> Handle(CreateRoleInput request, CancellationToken cancellationToken)
        {
            var role = Role.Create(request.RoleCode, request.RoleName, request.IsStatic ?? false, request.IsDefault ?? false);
            DbContext.Attach(role);
            await role.SaveAsync(cancellationToken);
            return role;
        }

        /// <inheritdoc />
        public async Task<Unit> Handle(SetRoleDefaultInput request, CancellationToken cancellationToken)
        {
            var originDefaultRoles = DbContext.Queryable<Role>().Where(x=>x.IsDefault);
            foreach (var originDefaultRole in originDefaultRoles)
            {
                originDefaultRole.IsDefault = false;
            }
            var role = DbContext.Queryable<Role>().FirstOrDefault(x=>x.Id == request.RoleId);
            role.IsDefault = true;
            return Unit.Value;
        }
    }
}
