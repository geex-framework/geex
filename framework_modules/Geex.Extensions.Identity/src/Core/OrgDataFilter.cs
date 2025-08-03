using System.Linq;
using Geex.Extensions.Authentication;
using MongoDB.Entities.Interceptors;

namespace Geex.Extensions.Identity.Core
{
    /// <summary>
    /// 组织架构的资源授权过滤器
    /// </summary>
    public class OrgDataFilter : ExpressionDataFilter<IOrgFilteredEntity>
    {
        public OrgDataFilter(ICurrentUser currentUser) : base(PredicateBuilder.New<IOrgFilteredEntity>(entity => currentUser.UserId == GeexConstants.SuperAdminId || entity.OrgCode == null || (currentUser.GetOrgCodes().Contains(entity.OrgCode))), null)
        {

        }
    }
}
