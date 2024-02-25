using MediatR;

namespace Geex.Common.Requests.MultiTenant;

public record ToggleTenantAvailabilityRequest : IRequest<bool>
{
    public string Code { get; set; }
}