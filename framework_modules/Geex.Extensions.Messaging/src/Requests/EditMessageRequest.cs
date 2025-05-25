using Geex.Extensions.Messaging.Api.Aggregates.Messages;
using KuanFang.Rms.MessageManagement.Messages;

using MediatR;

namespace Geex.Extensions.Requests.Messaging
{
    public record EditMessageRequest : IRequest
    {
        public string? Text { get; set; }
        public MessageSeverityType? Severity { get; set; }
        public string Id { get; set; }
        public MessageType? MessageType { get; set; }
    }
}
