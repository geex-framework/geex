using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication;

internal sealed class UserSessionContext : IUserSession
{
    private readonly IUnitOfWork _uow;
    private readonly Core.Utils.UserSessionService _sessionService;

    public UserSessionContext(IUnitOfWork uow, string userId)
    {
        _uow = uow;
        UserId = userId;
        _sessionService = uow.ServiceProvider.GetRequiredService<Core.Utils.UserSessionService>();
    }

    public string UserId { get; }

    public Task<DateTimeOffset> GetLastUpdatedOnAsync(CancellationToken cancellationToken = default)
        => _sessionService.GetLastUpdatedOnAsync(UserId, cancellationToken);

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
        => _sessionService.InvalidateAsync(UserId, cancellationToken);
}
