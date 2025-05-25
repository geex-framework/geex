using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Abstractions.Authentication;
using Geex.Abstractions.ClientNotification;
using Geex.Abstractions.Gql.Types;
using Geex.Abstractions;
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
