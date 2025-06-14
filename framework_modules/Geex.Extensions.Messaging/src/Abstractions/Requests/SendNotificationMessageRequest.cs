using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record SendNotificationMessageRequest : IRequest
    {
        public List<string> ToUserIds { get; set; }
        public string MessageId { get; set; }
    }
}
