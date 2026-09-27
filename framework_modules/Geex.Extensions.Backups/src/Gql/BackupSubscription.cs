using Geex.Extensions.Backups.Core.Entities;
using System.Runtime.CompilerServices;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Geex.Extensions.Backups.Gql;

public sealed class BackupSubscription : SubscriptionExtension<BackupSubscription>
{
    protected override void Configure(IObjectTypeDescriptor<BackupSubscription> descriptor)
    {
        descriptor.Field(x => x.OnBackupsChanged(default!, default)).Authorize(BackupsPermission.Query);
        base.Configure(descriptor);
    }

    [SubscribeAndResolve]
    public async IAsyncEnumerable<string> OnBackupsChanged([Service] ITopicEventReceiver receiver, [EnumeratorCancellation] CancellationToken token)
    {
        await using var stream = await receiver.SubscribeAsync<string>(Backup.ChangedTopic, token);
        yield return string.Empty;
        await foreach (var id in stream.ReadEventsAsync().WithCancellation(token)) yield return id;
    }
}
