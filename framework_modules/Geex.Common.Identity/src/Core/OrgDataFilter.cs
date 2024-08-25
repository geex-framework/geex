using System.Linq;
using System.Security.Claims;

using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstractions;

using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

namespace Geex.Common.Identity.Core
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
    /// <summary>
    /// 授权过滤接口
    /// </summary>
    public interface IOrgFilteredEntity : IEntityBase
    {
        public string OrgCode { get; }
    }
}
