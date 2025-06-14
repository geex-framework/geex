using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record MarkMessagesReadRequest : IRequest
    {
        public List<string> MessageIds { get; set; }
        public string UserId { get; set; }
    }
}
