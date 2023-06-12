using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.MultiTenant.Api;

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
