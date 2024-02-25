using Geex.Common.Abstraction.MultiTenant;
using MediatR;

namespace Geex.Common.Requests.MultiTenant;

public record EditTenantRequest : IRequest<ITenant>
{
    public string Code { get; set; }
    public string Name { get; set; }
}