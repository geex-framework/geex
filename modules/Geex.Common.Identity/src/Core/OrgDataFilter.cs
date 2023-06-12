using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using MongoDB.Driver;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

namespace Geex.Common.Identity.Core
{
    /// <summary>
    /// 组织架构的资源授权过滤器
    /// </summary>
    public class OrgDataFilter : ExpressionDataFilter<IOrgFilteredEntity>
    {
        public OrgDataFilter(LazyService<ClaimsPrincipal> claimsPrincipal) : base(PredicateBuilder.New<IOrgFilteredEntity>(entity => claimsPrincipal.Value.FindUserId() == "000000000000000000000001" || entity.OrgCode == null || claimsPrincipal.Value.FindOrgCodes().Contains(entity.OrgCode)), null)
        {

        }
    }
    /// <summary>
    /// 授权过滤接口
    /// </summary>
    public interface IOrgFilteredEntity : IEntityBase
    {
        public string OrgCode { get; }
    }
}
