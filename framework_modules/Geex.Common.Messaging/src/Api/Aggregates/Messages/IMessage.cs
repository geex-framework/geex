using System;
using System.Collections.Generic;
using Geex.Abstractions.MultiTenant;

using KuanFang.Rms.MessageManagement.Messages;

using MongoDB.Entities;

namespace Geex.Common.Messaging.Api.Aggregates.Messages
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public interface IMessage : IEntityBase, ITenantFilteredEntity
    {
        string? FromUserId { get; }
        public MessageType MessageType { get; }
        public IMessageContent Content { get; }
        IList<string> ToUserIds { get; }
        MessageSeverityType Severity { get; set; }
        public string Title { get; }
        public DateTimeOffset Time { get; }
    }
}
