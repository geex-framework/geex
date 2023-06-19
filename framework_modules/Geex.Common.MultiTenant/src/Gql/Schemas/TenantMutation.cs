using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Castle.Core.Internal;

using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.Authorization;
using Geex.Common.MultiTenant.Api;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests;
using Geex.Common.MultiTenant.Core;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.MultiTenant.Gql.Schemas
{
    public class TenantMutation : MutationExtension<TenantMutation>
    {
        private readonly IMediator _mediator;
        private readonly IRedisDatabase _redisDatabase;
        private readonly ICurrentTenant _currentTenant;
        private readonly IRbacEnforcer _enforcer;

        public TenantMutation(IMediator mediator, IRedisDatabase redisDatabase, ICurrentTenant currentTenant, IRbacEnforcer enforcer)
        {
            this._mediator = mediator;
            _redisDatabase = redisDatabase;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<TenantMutation> descriptor)
        {
            base.Configure(descriptor);
        }

        /// <summary>
        /// 创建Tenant
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ITenant> CreateTenant(CreateTenantRequest input)
        {
            var result = await _mediator.Send(input);
            return result;
        }

        /// <summary>
        /// 编辑Tenant
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditTenant(
            EditTenantRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        /// <summary>
        /// 切换Tenant可用状态
        /// </summary>
        /// <returns>当前租户的可用性</returns>
        public async Task<bool> ToggleTenantAvailability(
            ToggleTenantAvailabilityRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        /// <summary>
        /// 校验Tenant可用性
        /// </summary>
        /// <returns></returns>
        public async Task<ITenant?> CheckTenant(string code)
        {
            var result = (await _mediator.Send(new QueryInput<ITenant>(x => x.Code == code))).FirstOrDefault();
            if (result is not { ExternalInfo: null })
            {
                result = await _mediator.Send(new SyncExternalTenantRequest(code));
            }
            return result;
        }
    }
}
