using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Identity.Core.Entities;
using MediatX;
using Geex.Extensions.Identity.Requests;
using Geex.Requests;

namespace Geex.Extensions.Identity.Core.Handlers
{
    public class RoleHandler :
        IRequestHandler<QueryRequest<Role>, IQueryable<IRole>>,
        IRequestHandler<CreateRoleRequest, IRole>,
        IRequestHandler<UpdateRoleRequest, IRole>,
        IRequestHandler<CopyRoleRequest, IRole>,
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

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IRole> Handle(UpdateRoleRequest request, CancellationToken cancellationToken)
        {
            var role = await Uow.Query<Role>().OneAsync(request.Id, cancellationToken: cancellationToken);
            role.UpdateRole(request.Name, request.Code, request.Description, request.IsDefault, request.IsStatic, request.IsEnabled);
            return role;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IRole> Handle(CopyRoleRequest request, CancellationToken cancellationToken)
        {
            var fromRole = Uow.Query<Role>().FirstOrDefault(x => x.Code == request.FromRoleCode);
            if (fromRole == null)
                throw new BusinessException($"源角色 {request.FromRoleCode} 不存在");

            Role toRole;

            if (!request.ToRoleCode.IsNullOrEmpty())
            {
                // 检查目标角色是否存在
                toRole = Uow.Query<Role>().FirstOrDefault(x => x.Code == request.ToRoleCode);
                if (toRole != null)
                {
                    // 如果存在，通过Enforcer复制权限
                    toRole.CopyPermissionsFrom(fromRole);
                    return toRole;
                }
            }

            // 如果目标角色不存在，创建新角色
            var newRoleCode = request.ToRoleCode ?? $"{request.FromRoleCode}_Copy";
            var newRoleName = request.ToRoleName ?? $"{fromRole.Name}_Copy";

            toRole = new Role(newRoleCode, newRoleName, fromRole.IsStatic, false)
            {
                Description = fromRole.Description,
                IsEnabled = fromRole.IsEnabled
            };

            Uow.Attach(toRole);

            // 复制权限
            toRole.CopyPermissionsFrom(fromRole);

            return toRole;
        }
    }
}
