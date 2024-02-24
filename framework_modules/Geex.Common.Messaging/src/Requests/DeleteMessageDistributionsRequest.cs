using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Messaging.Requests
{
    public class DeleteMessageDistributionsRequest : IRequest
    {
        public string MessageId { get; set; }
        public List<string> UserIds { get; set; }
    }
}
