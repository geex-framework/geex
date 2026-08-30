using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Geex.Validation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Storage;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Messaging.Core.Entities;

/// <summary>
///     普通message
/// </summary>
public class Message : Entity<Message>, IMessage
{
    protected Message()
    {
    }

    public Message(string text, MessageSeverityType severity = MessageSeverityType.Info, JsonNode? meta = null)
        : this()
    {
        Title = text;
        Severity = severity;
        MessageType = MessageType.Notification;
        Meta = meta;
    }

    public Message(string text, IMessageContent content = default,
        MessageSeverityType severity = MessageSeverityType.Info, JsonNode? meta = null) : this(text, severity, meta)
    {
        Content = content;
        MessageType = content switch
        {
            ToDoContent => MessageType.Todo,
            InteractContent => MessageType.Interact,
            _ => MessageType.Notification
        };
    }

    public virtual IQueryable<MessageDistribution> Distributions =>
        DbContext.Query<MessageDistribution>().Where(x => x.MessageId == Id);

    private ILogger<Message> Logger => ServiceProvider.GetService<ILogger<Message>>();
    public IMessageContent Content { get; private set; }
    public string? FromUserId { get; private set; }

    public MessageType MessageType { get; set; }
    public MessageSeverityType Severity { get; set; }
    public DateTimeOffset Time => CreatedOn;
    public string Title { get; set; }
    public JsonNode? Meta { get; set; }
    public IList<string> ToUserIds => Distributions.ToList().Select(x => x.ToUserId).ToList();
    public string? TenantCode { get; set; }

    public async Task<Message> DistributeAsync(params string[] userIds)
    {
        if (userIds == null || userIds.Length == 0)
        {
            return this;
        }

        var distinctUserIds = userIds.Distinct().ToArray();
        var existingByUserId = Distributions
            .Where(x => distinctUserIds.Contains(x.ToUserId))
            .ToDictionary(x => x.ToUserId);

        foreach (var userId in distinctUserIds)
        {
            if (existingByUserId.TryGetValue(userId, out var distribution))
            {
                distribution.IsRead = false;
                continue;
            }

            DbContext.Attach(new MessageDistribution(Id, userId));
        }

        return this;
    }

    /// <summary>
    ///     标记当前消息针对特定用户已读
    /// </summary>
    /// <param name="userId"></param>
    public void MarkAsRead(string userId)
    {
        var userDistribution = Distributions.FirstOrDefault(x => x.ToUserId == userId);
        if (userDistribution != default)
            userDistribution.IsRead = true;
        else
            Logger.LogWarning("试图标记不存在的消息分配记录已读.");
    }
    public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }

        public class MessageBsonConfig : BsonConfig<Message>
    {
        protected override void Map(BsonClassMap<Message> map, BsonIndexConfig<Message> indexConfig)
        {
            map.Inherit<IMessage>();
            map.AutoMap();
            BsonClassMap.RegisterClassMap<InteractContent>();
            BsonClassMap.RegisterClassMap<ToDoContent>();
        }
    }
    public class MessageGqlConfig : GqlConfig.Object<Message>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Message> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<IMessage>>();
        }
    }
}
