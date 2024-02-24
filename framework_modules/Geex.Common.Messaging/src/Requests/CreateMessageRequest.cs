using Geex.Common.Messaging.Api.Aggregates.Messages;
using KuanFang.Rms.MessageManagement.Messages;
using MediatR;

namespace Geex.Common.Messaging.Requests
{
    public class CreateMessageRequest : IRequest<IMessage>
    {
        public string Text { get; set; }
        public MessageSeverityType Severity { get; set; }
    }
}
