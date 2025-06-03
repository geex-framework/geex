using System.Text.Json.Nodes;
using MediatR;

namespace Geex.Extensions.Messaging.Requests
{
    public record CreateMessageRequest : IRequest<IMessage>
    {
        public string Text { get; set; }
        public MessageSeverityType Severity { get; set; }
        public JsonNode? Meta { get; set; }
    }
}
