using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Messaging.Core.Sms;

public static class VirtualSmsStore
{
    public static ConcurrentBag<(string Phone, IReadOnlyList<string> Params)> Sent { get; } = new();
}

public class VirtualSmsSender : ISmsSender
{
    public Task SendAsync(string phoneNumber, IReadOnlyList<string> templateParams, CancellationToken cancellationToken = default)
    {
        VirtualSmsStore.Sent.Add((phoneNumber, templateParams));
        return Task.CompletedTask;
    }
}
