using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Abstractions.Authentication;
using Geex.Abstractions;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.Extensions.Messaging.Api.GqlSchemas.Messages
{
    public class MessageSubscription : SubscriptionExtension<MessageSubscription>
    {

    }
}
