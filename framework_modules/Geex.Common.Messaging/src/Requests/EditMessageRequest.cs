using Geex.Common.Messaging.Api.Aggregates.Messages;
using KuanFang.Rms.MessageManagement.Messages;

using MediatR;

namespace Geex.Common.Requests.Messaging
{
    public class EditMessageRequest : IRequest
    {
        public string? Text { get; set; }
        public MessageSeverityType? Severity { get; set; }
        public string Id { get; set; }
        public MessageType? MessageType { get; set; }
    }
}