using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Geex.Common.Messaging.Api.Aggregates.Messages.Inputs
{
    public class MarkMessagesReadInput:IRequest
    {
        public List<string> MessageIds { get; set; }
        public string UserId { get; set; }
    }
}
