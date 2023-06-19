using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Castle.Core.Internal;

using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.Authorization;
using Geex.Common.MultiTenant.Api;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests;
using Geex.Common.MultiTenant.Core;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.MultiTenant.Gql.Schemas
{
    public class TenantQuery : QueryExtension<TenantQuery>
    {
        private readonly IMediator _mediator;
        private readonly IRedisDatabase _redisDatabase;
        private readonly ICurrentTenant _currentTenant;
        private readonly IRbacEnforcer _enforcer;

        public TenantQuery(IMediator mediator, IRedisDatabase redisDatabase, ICurrentTenant currentTenant, IRbacEnforcer enforcer)
        {
            this._mediator = mediator;
            _redisDatabase = redisDatabase;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
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
            var result = await _mediator.Send(new QueryInput<ITenant>());
            return result;
        }
    }
}
