using System.Linq;
using Geex.Abstractions.MultiTenant;
using Geex.Abstractions;
using MongoDB.Entities.Interceptors;

namespace Geex.Common.MultiTenant.Core
{
    public class TenantDataFilter : ExpressionDataFilter<ITenantFilteredEntity>
    {
        /// <inheritdoc />
        public TenantDataFilter(LazyService<ICurrentTenant> currentTenant) : base(PredicateBuilder.New<ITenantFilteredEntity>(entity => currentTenant.Value.Code == entity.TenantCode), null)
        {
        }
    }
}
