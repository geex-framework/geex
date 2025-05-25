using System.Collections.Generic;
using MediatR;

namespace Geex.Extensions.Requests.Messaging
{
    public record SendNotificationMessageRequest : IRequest
    {
        public List<string> ToUserIds { get; set; }
        public string MessageId { get; set; }
    }
}
