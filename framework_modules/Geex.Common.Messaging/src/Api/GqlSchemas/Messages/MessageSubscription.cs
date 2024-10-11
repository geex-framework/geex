using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.ClientNotification;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages
{
    public class MessageSubscription : SubscriptionExtension<MessageSubscription>
    {

    }
}
