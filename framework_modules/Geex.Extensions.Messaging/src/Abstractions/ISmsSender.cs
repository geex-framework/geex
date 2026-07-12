using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Messaging;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, IReadOnlyList<string> templateParams, CancellationToken cancellationToken = default);
}
