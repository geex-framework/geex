using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.ClientNotification;
using Geex.Extensions.Messaging.ClientNotification;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Messaging
{
    public static class Extensions
    {
        public static async Task ClientNotify<T>(this IUnitOfWork uow, T clientNotify, params string[] userIds) where T : ClientNotify
        {
            var topicEventSender = uow.ServiceProvider.GetService<ITopicEventSender>();
            if (!userIds.IsNullOrEmpty())
            {
                await Task.WhenAll(userIds.Select(userId => topicEventSender.SendAsync($"{nameof(ClientNotifySubscription.OnPrivateNotify)}:{userId}", clientNotify).AsTask()));
            }
            else
            {
                await topicEventSender.SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), clientNotify as ClientNotify);
            }
        }
    }
}
