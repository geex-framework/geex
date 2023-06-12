
using Geex.Common.Abstraction.MultiTenant;
using MediatR;

namespace Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests;

public record EditTenantRequest : IRequest<ITenant>
{
    public string Code { get; set; }
    public string Name { get; set; }
}