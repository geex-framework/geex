using System.Text.Json.Nodes;


using MongoDB.Entities;

namespace Geex.MultiTenant
{
    public interface ITenant : IEntityBase
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public JsonNode? ExternalInfo { get; set; }
    }
}
