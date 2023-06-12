using System.Collections.Generic;

using MediatR;

namespace Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests;

public record ToggleTenantAvailabilityRequest : IRequest<bool>
{
    public string Code { get; set; }
}