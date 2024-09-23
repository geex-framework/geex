﻿using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Requests.Messaging
{
    public record MarkMessagesReadRequest : IRequest
    {
        public List<string> MessageIds { get; set; }
        public string UserId { get; set; }
    }
}
