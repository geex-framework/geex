using System.Linq;
using System.Security.Claims;

using Geex.Abstractions.Authentication;
using Geex.Abstractions;
using MongoDB.Entities.Interceptors;

namespace Geex.Extensions.Identity.Core
{
    /// <summary>
    /// 组织架构的资源授权过滤器
    /// </summary>
    public class OrgDataFilter : ExpressionDataFilter<IOrgFilteredEntity>
    {
        public OrgDataFilter(ICurrentUser currentUser) : base(PredicateBuilder.New<IOrgFilteredEntity>(entity => currentUser.UserId == "000000000000000000000001" || entity.OrgCode == null || (currentUser.User != null && currentUser.User.OrgCodes.Contains(entity.OrgCode))), null)
        {

        }
    }
}
