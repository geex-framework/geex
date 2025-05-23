﻿using System.Text.Json.Nodes;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using KuanFang.Rms.MessageManagement.Messages;
using MediatR;

namespace Geex.Common.Requests.Messaging
{
    public record CreateMessageRequest : IRequest<IMessage>
    {
        public string Text { get; set; }
        public MessageSeverityType Severity { get; set; }
        public JsonNode? Meta { get; set; }
    }
}
