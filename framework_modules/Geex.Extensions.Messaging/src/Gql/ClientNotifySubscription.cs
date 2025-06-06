using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Extensions.Messaging.ClientNotification;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.ClientNotification
{
    public class ClientNotifySubscription : SubscriptionExtension<ClientNotifySubscription>
    {
        /// <summary>
        /// 订阅服务器对单个用户的前端调用
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="claimsPrincipal"></param>
        /// <returns></returns>
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<ClientNotify>> OnPrivateNotify([Service] ITopicEventReceiver receiver, [Service] ClaimsPrincipal claimsPrincipal)
        {
            return receiver.SubscribeAsync<ClientNotify>($"{nameof(OnPrivateNotify)}:{claimsPrincipal.FindUserId()}");
        }

        /// <summary>
        /// 订阅广播
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<ClientNotify>> OnPublicNotify([Service] ITopicEventReceiver receiver)
        {
            return receiver.SubscribeAsync<ClientNotify>(nameof(OnPublicNotify));
        }

        /// <summary>
        /// 测试
        /// </summary>
        /// <returns></returns>
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<string>> Echo(string text, [Service] ITopicEventReceiver receiver, [Service] ITopicEventSender sender)
        {
            Task.Run(async () =>
            {
                for (var i = 0; i < 2; i++)
                {
                    await Task.Delay(1000);
                    await sender.SendAsync(nameof(Echo), text);
                }
                await sender.CompleteAsync(nameof(Echo));
            });
            return receiver.SubscribeAsync<string>(nameof(Echo));
        }
    }
}
