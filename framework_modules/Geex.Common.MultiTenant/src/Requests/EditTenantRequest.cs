using Geex.Common.Abstraction.MultiTenant;
using MediatR;

namespace Geex.Common.MultiTenant.Requests;

public record EditTenantRequest : IRequest<ITenant>
{
    public string Code { get; set; }
    public string Name { get; set; }
}