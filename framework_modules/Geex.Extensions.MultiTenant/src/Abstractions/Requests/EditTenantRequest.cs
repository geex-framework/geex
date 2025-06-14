using Geex.MultiTenant;
using MediatX;

namespace Geex.Extensions.Requests.MultiTenant;

public record EditTenantRequest : IRequest<ITenant>
{
    public string Code { get; set; }
    public string Name { get; set; }
}
