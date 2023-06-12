using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.MultiTenant.Api;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using MongoDB.Entities;

using Volo.Abp;

namespace Geex.Common.MultiTenant.Core
{
    public class CurrentTenant : ICurrentTenant
    {
        private readonly ICurrentTenantResolver _currentTenantResolver;
        private readonly DbContext _dbContext;
        private string? _tenantCode;
        private ITenant? _detail;
        private readonly Queue<string?> _parentScopes = new Queue<string?>();

        /// <inheritdoc />
        public string? Code => _tenantCode ?? _currentTenantResolver.Resolve();

        /// <inheritdoc />
        public ITenant Detail => _detail ??= _dbContext.Queryable<Tenant>().FirstOrDefault(x => x.Code == Code);


        public CurrentTenant(ICurrentTenantResolver currentTenantResolver, DbContext dbContext)
        {
            _currentTenantResolver = currentTenantResolver;
            _dbContext = dbContext;
        }
        /// <inheritdoc />
        public virtual IDisposable Change(string? tenantCode)
        {
            return this.SetCurrent(tenantCode);
        }

        private IDisposable SetCurrent(string? tenantCode)
        {
            this._parentScopes.Enqueue(Code);
            this._tenantCode = tenantCode;
            this._detail = _dbContext.Queryable<Tenant>().FirstOrDefault(x => x.Code == tenantCode);
            return new DisposeAction(() =>
            {
                _tenantCode = _parentScopes.Dequeue();
            });
        }
    }
}
