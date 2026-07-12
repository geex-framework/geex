using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.Extensions.MultiTenant.Core.Providers;

using MediatX;
using Geex.Extensions.Requests.MultiTenant;
using Geex.MultiTenant;

namespace Geex.Extensions.MultiTenant.Core.Handlers
{
    public class TenantHandler : ICommonHandler<ITenant, Tenant>,
        IRequestHandler<CreateTenantRequest, ITenant>,
        IRequestHandler<EditTenantRequest, ITenant>,
        IRequestHandler<ToggleTenantAvailabilityRequest, bool>,
        IRequestHandler<SyncExternalTenantRequest, ITenant>,
        IRequestHandler<DeleteTenantRequest, bool>
    {

        /// <inheritdoc />
        public IUnitOfWork Uow { get; }
        private readonly IExternalTenantSyncProvider _externalTenantSyncProvider;

        public TenantHandler(IUnitOfWork uow, IExternalTenantSyncProvider externalTenantSyncProvider)
        {
            this.Uow = uow;
            _externalTenantSyncProvider = externalTenantSyncProvider;
        }

        /// <inheritdoc />
        public async Task<ITenant> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = Uow.Create(request);
            return tenant;
        }

        /// <inheritdoc />
        public async Task<ITenant> Handle(EditTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = this.Uow.Query<ITenant>().FirstOrDefault(x => x.Code == request.Code);
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }
            tenant.Name = request.Name;
            return tenant;
        }

        /// <inheritdoc />
        public async Task<bool> Handle(ToggleTenantAvailabilityRequest request, CancellationToken cancellationToken)
        {
            var tenant = this.Uow.Query<ITenant>().FirstOrDefault(x => x.Code == request.Code);
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }
            tenant.IsEnabled = !tenant.IsEnabled;
            return tenant.IsEnabled;
        }

        public async Task<ITenant> Handle(SyncExternalTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = this.Uow.Query<ITenant>().FirstOrDefault(x => x.Code == request.Code);
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }

            return await _externalTenantSyncProvider.SyncAsync(request.Code, tenant, cancellationToken);
        }

        public async Task<bool> Handle(DeleteTenantRequest request, CancellationToken cancellationToken)
        {
            using var _ = Uow.DbContext.DisableAllDataFilters();
            var tenant = Uow.Query<Tenant>().FirstOrDefault(x => x.Code == request.Code);
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }

            if (Uow.Query<IUser>().Any(x => x.TenantCode == request.Code))
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: $"租户[{request.Code}]下仍有关联用户，无法删除");
            }

            await Uow.DeleteAsync<Tenant>(x => x.Code == request.Code, cancellationToken);
            return true;
        }
    }
}
