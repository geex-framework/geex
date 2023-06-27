using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Messaging.Api.Aggregates.Messages;

using HotChocolate.Types;

using KuanFang.Rms.MessageManagement.Messages;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson.Serialization;

namespace Geex.Common.Messaging.Core.Aggregates.Messages;

/// <summary>
///     普通message
/// </summary>
public class Message : Entity<Message>, IMessage
{
    protected Message()
    {
    }

    public Message(string text, MessageSeverityType severity = MessageSeverityType.Info)
        : this()
    {
        Title = text;
        Severity = severity;
        MessageType = MessageType.Notification;
    }

    public Message(string text, IMessageContent content = default,
        MessageSeverityType severity = MessageSeverityType.Info) : this(text, severity)
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
        DbContext.Queryable<MessageDistribution>().Where(x => x.MessageId == Id);

    private ILogger<Message> Logger => ServiceProvider.GetService<ILogger<Message>>();
    public IMessageContent Content { get; private set; }
    public string? FromUserId { get; private set; }

    public MessageType MessageType { get; set; }
    public MessageSeverityType Severity { get; set; }
    public DateTimeOffset Time => CreatedOn;
    public string Title { get; set; }
    public IList<string> ToUserIds => Distributions.ToList().Select(x => x.ToUserId).ToList();
    public string? TenantCode { get; set; }

    public async Task<Message> DistributeAsync(params string[] userIds)
    {
        if (Distributions.Any()) return this;

        var distributions = userIds.Select(x => new MessageDistribution(Id, x)).ToList();
        DbContext.Attach(distributions);

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
    public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }

        public class MessageBsonConfig : BsonConfig<Message>
    {
        protected override void Map(BsonClassMap<Message> map)
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
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.FromUserId);
            //descriptor.Field(x => x.MessageType);
            //descriptor.Field(x => x.Content);
            //descriptor.Field(x => x.ToUserIds);
            //descriptor.Field(x => x.Id);
            //descriptor.Field(x => x.Title);
            //descriptor.Field(x => x.Time);
            //descriptor.Field(x => x.Severity);
            descriptor.Implements<InterfaceType<IMessage>>();
        }
    }
}
