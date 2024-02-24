using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Requests;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.MultiTenant.Gql.Schemas
{
    public class TenantQuery : QueryExtension<TenantQuery>
    {
        private readonly IMediator _mediator;

        public TenantQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

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

        /// <summary>
        /// 列表获取Tenant
        /// </summary>
        /// <returns></returns>
        public async Task<IQueryable<ITenant>> Tenants()
        {
            var result = await _mediator.Send(new QueryRequest<ITenant>());
            return result;
        }
    }
}
