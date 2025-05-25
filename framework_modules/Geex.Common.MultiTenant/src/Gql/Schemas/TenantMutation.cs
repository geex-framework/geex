using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions.Authorization;
using Geex.Common.Requests;
using Geex.Abstractions.Gql.Types;
using Geex.Abstractions.MultiTenant;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;
using Geex.Common.Requests.MultiTenant;
using Geex.Abstractions;

namespace Geex.Common.MultiTenant.Gql.Schemas
{
    public sealed class TenantMutation : MutationExtension<TenantMutation>
    {
        private readonly IUnitOfWork _uow;

        public TenantMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 创建Tenant
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ITenant> CreateTenant(CreateTenantRequest request)
        {
            var result = await _uow.Request(request);
            return result;
        }

        /// <summary>
        /// 编辑Tenant
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ITenant> EditTenant(EditTenantRequest request) => await _uow.Request(request);

        /// <summary>
        /// 切换Tenant可用状态
        /// </summary>
        /// <returns>当前租户的可用性</returns>
        public async Task<bool> ToggleTenantAvailability(ToggleTenantAvailabilityRequest request) => await _uow.Request(request);

        /// <summary>
        /// 校验Tenant可用性
        /// </summary>
        /// <returns></returns>
        public async Task<ITenant?> CheckTenant(string code)
        {
            var result = (await _uow.Request(new QueryRequest<ITenant>(x => x.Code == code))).FirstOrDefault();
            if (result is not { ExternalInfo: null })
            {
                result = await _uow.Request(new SyncExternalTenantRequest(code));
            }
            return result;
        }
    }
}
