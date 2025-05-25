using Geex.MultiTenant;
using MediatR;

namespace Geex.Extensions.Requests.MultiTenant;

public record EditTenantRequest : IRequest<ITenant>
{
    public string Code { get; set; }
    public string Name { get; set; }
}
