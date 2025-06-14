using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record EditMessageRequest : IRequest
    {
        public string? Text { get; set; }
        public MessageSeverityType? Severity { get; set; }
        public string Id { get; set; }
        public MessageType? MessageType { get; set; }
    }
}
