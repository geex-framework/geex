using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Api.GqlSchemas.Messages;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;

using HotChocolate.Subscriptions;
using MediatR;

namespace Geex.Bms.Core.CacheData
{
    public class CacheDataHandler :
        INotificationHandler<OrgCodeChangedEvent>,
        INotificationHandler<EntityCreatedNotification<Org>>,
        INotificationHandler<EntityDeletedNotification<Org>>
    {
        private readonly ITopicEventSender _topicEventSender;

        public CacheDataHandler(ITopicEventSender topicEventSender)
        {
            _topicEventSender = topicEventSender;
        }

        /// <inheritdoc />
        public async Task Handle(OrgCodeChangedEvent notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(CacheDataType.Org);
        }

        /// <inheritdoc />
        public async Task Handle(EntityCreatedNotification<Org> notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(CacheDataType.Org);
        }

        /// <inheritdoc />
        public async Task Handle(EntityDeletedNotification<Org> notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(CacheDataType.Org);
        }

        private async Task NotifyCacheDataChange(CacheDataType type)
        {
            // bug:这里的type无法正常序列化为枚举, 暂时toString
            await this._topicEventSender.SendAsync<IFrontendCall>(nameof(MessageSubscription.OnBroadcast), new FrontendCall(BmsFrontCallType.CacheDataChange, JsonSerializer.SerializeToNode(new { Type = type.ToString() })));
        }
    }
}
