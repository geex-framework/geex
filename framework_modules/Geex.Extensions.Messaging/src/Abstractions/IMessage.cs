using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using MongoDB.Entities;

namespace Geex.Extensions.Messaging
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public interface IMessage : IEntityBase
    {
        string? FromUserId { get; }
        public MessageType MessageType { get; }
        public IMessageContent Content { get; }
        IList<string> ToUserIds { get; }
        MessageSeverityType Severity { get; set; }
        public string Title { get; }
        public DateTimeOffset Time { get; }
        /// <summary>
        /// Optional structured payload (e.g. ApprovalFlowId for deep links).
        /// </summary>
        public JsonNode? Meta { get; }
    }
}
