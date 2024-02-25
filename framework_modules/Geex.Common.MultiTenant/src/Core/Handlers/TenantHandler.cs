using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Requests;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using MediatR;

using MongoDB.Entities;
using Geex.Common.MultiTenant.Requests;

namespace Geex.Common.MultiTenant.Core.Handlers
{
    public class TenantHandler : ICommonHandler<ITenant, Tenant>,
        IRequestHandler<CreateTenantRequest, ITenant>,
        IRequestHandler<EditTenantRequest, ITenant>,
        IRequestHandler<ToggleTenantAvailabilityRequest, bool>
    {
        private readonly IMediator _mediator;

        /// <inheritdoc />
        public IUnitOfWork Uow { get; }

        public TenantHandler(IUnitOfWork uow, IMediator mediator)
        {
            this.Uow = uow;
            this._mediator = mediator;
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
            var tenant = (await this._mediator.Send(new QueryRequest<ITenant>(x => x.Code == request.Code), cancellationToken)).FirstOrDefault();
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
            var tenant = (await this._mediator.Send(new QueryRequest<ITenant>(x => x.Code == request.Code), cancellationToken)).FirstOrDefault();
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }
            tenant.IsEnabled = !tenant.IsEnabled;
            return tenant.IsEnabled;
        }
    }
}
