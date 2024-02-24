using MediatR;

namespace Geex.Common.MultiTenant.Requests;

public record ToggleTenantAvailabilityRequest : IRequest<bool>
{
    public string Code { get; set; }
}