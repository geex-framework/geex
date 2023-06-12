using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Castle.Core.Internal;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Api.GqlSchemas.Orgs.Inputs;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;

using MediatR;

using MongoDB.Entities;

using DbContext = MongoDB.Entities.DbContext;

namespace Geex.Common.Identity.Core.Handlers
{
    public class OrgHandler :
        IRequestHandler<QueryInput<Org>, IQueryable<Org>>,
        IRequestHandler<CreateOrgInput, Org>,
        IRequestHandler<FixUserOrgInput, bool>
    {
        private readonly LazyService<ClaimsPrincipal> _principalFactory;
        public DbContext DbContext { get; }

        public OrgHandler(DbContext dbContext, LazyService<ClaimsPrincipal> principalFactory)
        {
            _principalFactory = principalFactory;
            DbContext = dbContext;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IQueryable<Org>> Handle(QueryInput<Org> request, CancellationToken cancellationToken)
        {
            return DbContext.Queryable<Org>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Org> Handle(CreateOrgInput request, CancellationToken cancellationToken)
        {
            var entity = new Org(request.Code, request.Name, request.OrgType);
            DbContext.Attach(entity);
            var userId = request.CreateUserId;
            // 区域创建者自动拥有Org权限
            if (!userId.IsNullOrEmpty())
            {
                var user = await DbContext.Queryable<User>().OneAsync(userId, cancellationToken: cancellationToken);
                await user.AddOrg(entity);
            }

            // 拥有上级Org权限的用户自动获得新增子Org的权限
            var upperUsers = DbContext.Queryable<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
            foreach (var upperUser in upperUsers)
            {
                await upperUser.AddOrg(entity);
            }

            return entity;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<bool> Handle(FixUserOrgInput request, CancellationToken cancellationToken)
        {
            try
            {
                using var _ = DbContext.DisableAllDataFilters();
                var userList = DbContext.Queryable<User>().ToList();
                foreach (var user in userList.Where(x => !x.OrgCodes.Any()))
                {
                    if (user.TenantCode.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    var entity = DbContext.Queryable<Org>()
                        .FirstOrDefault(x => x.TenantCode == user.TenantCode && x.Name == "集团总部");
                    if (entity is null)
                    {
                        entity = new Org(user.TenantCode, "公司总部", OrgTypeEnum.Default)
                        {
                            TenantCode = user.TenantCode
                        };
                        DbContext.Attach(entity);
                    }
                    await user.AddOrg(entity);
                    // 拥有上级Org权限的用户自动获得新增子Org的权限
                    var upperUsers = DbContext.Queryable<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
                    foreach (var upperUser in upperUsers)
                    {
                        await upperUser.AddOrg(entity);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
        }
    }
}
