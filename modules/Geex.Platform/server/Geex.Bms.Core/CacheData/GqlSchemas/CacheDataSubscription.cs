using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.Bms.Core.CacheData.GqlSchemas
{
    public class CacheDataSubscription : SubscriptionExtension<CacheDataSubscription>
    {
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<IFrontendCall>> OnCacheDataChange([Service] ITopicEventReceiver receiver)
        {
            return receiver.SubscribeAsync<IFrontendCall>(BmsFrontCallType.CacheDataChange);
        }
    }
}
