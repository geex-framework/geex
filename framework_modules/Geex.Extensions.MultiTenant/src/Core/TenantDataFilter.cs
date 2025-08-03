using System.Linq;
using Geex.MultiTenant;
using MongoDB.Entities.Interceptors;

namespace Geex.Extensions.MultiTenant.Core
{
    public class TenantDataFilter : ExpressionDataFilter<ITenantFilteredEntity>
    {
        /// <inheritdoc />
        public TenantDataFilter(LazyService<ICurrentTenant> currentTenant) : base(PredicateBuilder.New<ITenantFilteredEntity>(entity => currentTenant.Value!.Code == entity.TenantCode)!, null)
        {
        }
    }
}
