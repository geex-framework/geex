using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Requests;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;

using MediatR;

using MongoDB.Entities;
using Geex.Extensions.Requests.MultiTenant;
using Geex.MultiTenant;

namespace Geex.Extensions.MultiTenant.Core.Handlers
{
    public class TenantHandler : ICommonHandler<ITenant, Tenant>,
        IRequestHandler<CreateTenantRequest, ITenant>,
        IRequestHandler<EditTenantRequest, ITenant>,
        IRequestHandler<ToggleTenantAvailabilityRequest, bool>
    {

        /// <inheritdoc />
        public IUnitOfWork Uow { get; }

        public TenantHandler(IUnitOfWork uow)
        {
            this.Uow = uow;
        }

        /// <inheritdoc />
        public async Task<ITenant> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(request.Code, request.Name, request.ExternalInfo);
            Uow.Attach(tenant);
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
    }
}
