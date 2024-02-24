using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Messaging.Requests
{
    public class MarkMessagesReadRequest : IRequest
    {
        public List<string> MessageIds { get; set; }
        public string UserId { get; set; }
    }
}
