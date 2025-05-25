using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Abstractions.Authorization;
using Geex.Common.Requests;
using Geex.Abstractions.Gql.Types;
using Geex.Abstractions.MultiTenant;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.MultiTenant.Gql.Schemas
{
    public sealed class TenantQuery : QueryExtension<TenantQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<TenantQuery> descriptor)
        {
            descriptor.Field(x => x.Tenants())
                .UseOffsetPaging()
                .UseFiltering()
                //.Authorize(MultiTenantPermissions.TenantPermissions.Query)
                ;
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public TenantQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 列表获取Tenant
        /// </summary>
        /// <returns></returns>
        public async Task<IQueryable<ITenant>> Tenants()
        {
            var result = await _uow.Request(new QueryRequest<ITenant>());
            return result;
        }
    }
}
