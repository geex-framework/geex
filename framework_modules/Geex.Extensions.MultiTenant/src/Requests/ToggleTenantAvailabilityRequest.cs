using MediatR;

namespace Geex.Extensions.Requests.MultiTenant;

public record ToggleTenantAvailabilityRequest : IRequest<bool>
{
    public string Code { get; set; }
}
