using System.Text.Json.Nodes;
using Geex.MultiTenant;
using MediatX;

namespace Geex.Extensions.Requests.MultiTenant
{
    public record CreateTenantRequest : IRequest<ITenant>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public JsonNode? ExternalInfo { get; set; }

    }
}
