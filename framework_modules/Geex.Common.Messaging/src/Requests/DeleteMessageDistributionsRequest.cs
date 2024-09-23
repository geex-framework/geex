using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Requests.Messaging
{
    public record DeleteMessageDistributionsRequest : IRequest
    {
        public string MessageId { get; set; }
        public List<string> UserIds { get; set; }
    }
}
