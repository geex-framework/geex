using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Geex.Common.Messaging.Api.Aggregates.Messages.Inputs
{
    public class DeleteMessageDistributionsInput:IRequest
    {
        public string MessageId { get; set; }
        public List<string> UserIds { get; set; }
    }
}
