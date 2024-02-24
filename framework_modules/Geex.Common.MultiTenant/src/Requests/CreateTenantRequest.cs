using System.Text.Json.Nodes;
using Geex.Common.Abstraction.MultiTenant;
using MediatR;

namespace Geex.Common.MultiTenant.Requests
{
    public record CreateTenantRequest : IRequest<ITenant>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public JsonNode? ExternalInfo { get; set; }

    }
}
