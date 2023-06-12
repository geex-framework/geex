using System.Security;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;

using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages
{
    public class MessageSubscription : SubscriptionExtension<MessageSubscription>
    {
        /// <summary>
        /// 订阅服务器对单个用户的前端调用
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="claimsPrincipal"></param>
        /// <returns></returns>
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<IFrontendCall>> OnFrontendCall([Service] ITopicEventReceiver receiver, [Service] LazyService<ClaimsPrincipal> claimsPrincipal)
        {
            return receiver.SubscribeAsync<IFrontendCall>($"{nameof(OnFrontendCall)}:{claimsPrincipal.Value?.FindUserId()}");
        }

        /// <summary>
        /// 订阅广播
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<IFrontendCall>> OnBroadcast([Service] ITopicEventReceiver receiver)
        {
            return receiver.SubscribeAsync<IFrontendCall>(nameof(OnBroadcast));
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
                for (int i = 0; i < 2; i++)
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
