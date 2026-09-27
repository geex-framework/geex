using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Messaging.ClientNotification;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;

namespace Geex.ClientNotification;

public class ClientNotifySubscription : SubscriptionExtension<ClientNotifySubscription>
{
    [SubscribeAndResolve]
    public IAsyncEnumerable<ClientNotify> OnPrivateNotify([Service] ITopicEventReceiver receiver,
        [Service] ClaimsPrincipal claimsPrincipal, [Service] IServiceScopeFactory scopes, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.FindUserId();
        if (string.IsNullOrWhiteSpace(userId)) throw new GraphQLException("Authentication required.");
        return Receive(receiver, scopes, $"{nameof(OnPrivateNotify)}:{userId}", cancellationToken);
    }

    [SubscribeAndResolve]
    public IAsyncEnumerable<ClientNotify> OnPublicNotify([Service] ITopicEventReceiver receiver,
        [Service] IServiceScopeFactory scopes, CancellationToken cancellationToken) =>
        Receive(receiver, scopes, nameof(OnPublicNotify), cancellationToken);

    private static async IAsyncEnumerable<ClientNotify> Receive(ITopicEventReceiver receiver,
        IServiceScopeFactory scopes, string topic, [EnumeratorCancellation] CancellationToken token)
    {
        await using var stream = await receiver.SubscribeAsync<ClientNotifyEnvelope>(topic, token);
        yield return new SubscriptionReadyClientNotify();
        await foreach (var envelope in stream.ReadEventsAsync().WithCancellation(token))
        {
            using var scope = scopes.CreateScope();
            ClientNotify notification;
            if (envelope.Kind == nameof(NewMessageClientNotify))
            {
                var message = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Query<Message>().OneAsync(envelope.Value);
                if (message == null) continue;
                notification = new NewMessageClientNotify(message);
            }
            else if (envelope.Kind == nameof(DataChangeClientNotify))
                notification = new DataChangeClientNotify(DataChangeType.FromValue(envelope.Value));
            else continue;
            notification.CreatedOn = envelope.CreatedOn;
            yield return notification;
        }
    }

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
