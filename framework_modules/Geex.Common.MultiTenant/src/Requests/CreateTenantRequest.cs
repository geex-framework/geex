using System.Text.Json.Nodes;
using Geex.Abstractions.MultiTenant;
using MediatR;

namespace Geex.Common.Requests.MultiTenant
{
    public record CreateTenantRequest : IRequest<ITenant>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public JsonNode? ExternalInfo { get; set; }

    }
}
