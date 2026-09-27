using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.ClientNotification;
using Geex.Extensions.Messaging.ClientNotification;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Messaging
{
    public static class Extensions
    {
        public static Task ClientNotify<T>(this IUnitOfWork uow, T clientNotify, params string[] userIds) where T : ClientNotify =>
            uow.ClientNotify(clientNotify, CancellationToken.None, userIds);

        public static async Task ClientNotify<T>(this IUnitOfWork uow, T clientNotify, CancellationToken token, params string[] userIds) where T : ClientNotify
        {
            var topicEventSender = uow.ServiceProvider.GetRequiredService<ITopicEventSender>();
            if (!userIds.IsNullOrEmpty())
            {
                await Task.WhenAll(userIds.Select(userId => topicEventSender.SendAsync($"{nameof(ClientNotifySubscription.OnPrivateNotify)}:{userId}", ClientNotifyEnvelope.From(clientNotify), token).AsTask()));
            }
            else
            {
                await topicEventSender.SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), ClientNotifyEnvelope.From(clientNotify), token);
            }
        }
    }
}
