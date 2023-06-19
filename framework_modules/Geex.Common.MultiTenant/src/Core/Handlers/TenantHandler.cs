using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Auditing.Events;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.MultiTenant.Core.Handlers
{
    public class TenantHandler : ICommonHandler<ITenant, Tenant>,
        IRequestHandler<CreateTenantRequest, ITenant>,
        IRequestHandler<EditTenantRequest, ITenant>,
        IRequestHandler<ToggleTenantAvailabilityRequest, bool>
    {
        private readonly IMediator _mediator;

        /// <inheritdoc />
        public DbContext DbContext { get; }

        public TenantHandler(DbContext dbContext, IMediator mediator)
        {
            this.DbContext = dbContext;
            this._mediator = mediator;
        }

        /// <inheritdoc />
        public async Task<ITenant> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(request.Code, request.Name, request.ExternalInfo);
            DbContext.Attach(tenant);
            return tenant;
        }

        /// <inheritdoc />
        public async Task<ITenant> Handle(EditTenantRequest request, CancellationToken cancellationToken)
        {
            var tenant = (await this._mediator.Send(new QueryInput<ITenant>(x => x.Code == request.Code), cancellationToken)).FirstOrDefault();
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
            var tenant = (await this._mediator.Send(new QueryInput<ITenant>(x => x.Code == request.Code), cancellationToken)).FirstOrDefault();
            if (tenant == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: $"租户不存在[{request.Code}]");
            }
            tenant.IsEnabled = !tenant.IsEnabled;
            return tenant.IsEnabled;
        }
    }
}
