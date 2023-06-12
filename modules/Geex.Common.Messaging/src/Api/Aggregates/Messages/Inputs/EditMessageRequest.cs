using KuanFang.Rms.MessageManagement.Messages;

using MediatR;

namespace Geex.Common.Messaging.Api.Aggregates.Messages.Inputs
{
    public class EditMessageRequest : IRequest<Unit>
    {
        public string? Text { get; set; }
        public MessageSeverityType? Severity { get; set; }
        public string Id { get; set; }
        public MessageType? MessageType { get; set; }
    }
}