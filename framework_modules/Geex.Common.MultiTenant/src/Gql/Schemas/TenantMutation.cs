using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Requests;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;
using Geex.Common.Requests.MultiTenant;

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
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ITenant> CreateTenant(CreateTenantRequest request)
        {
            var result = await _mediator.Send(request);
            return result;
        }

        /// <summary>
        /// 编辑Tenant
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> EditTenant(
            EditTenantRequest request)
        {
            var result = await _mediator.Send(request);
            return true;
        }

        /// <summary>
        /// 切换Tenant可用状态
        /// </summary>
        /// <returns>当前租户的可用性</returns>
        public async Task<bool> ToggleTenantAvailability(
            ToggleTenantAvailabilityRequest request)
        {
            var result = await _mediator.Send(request);
            return true;
        }

        /// <summary>
        /// 校验Tenant可用性
        /// </summary>
        /// <returns></returns>
        public async Task<ITenant?> CheckTenant(string code)
        {
            var result = (await _mediator.Send(new QueryRequest<ITenant>(x => x.Code == code))).FirstOrDefault();
            if (result is not { ExternalInfo: null })
            {
                result = await _mediator.Send(new SyncExternalTenantRequest(code));
            }
            return result;
        }
    }
}
