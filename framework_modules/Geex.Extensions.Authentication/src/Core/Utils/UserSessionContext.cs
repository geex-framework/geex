using System;
using System.Linq;
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

    public async Task<UserSession> BeginAsync(LoginProviderEnum provider, string token, CancellationToken cancellationToken = default)
    {
        var user = _uow.Query<IAuthUser>().FirstOrDefault(x => x.Id == UserId)
            ?? throw new BusinessException(GeexExceptionType.NotFound, message: "User not found.");
        return await _sessionService.BeginAsync(user, provider, token, cancellationToken);
    }

    public Task<DateTimeOffset> GetLastUpdatedOnAsync(CancellationToken cancellationToken = default)
        => _sessionService.GetLastUpdatedOnAsync(UserId, cancellationToken);

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
        => _sessionService.InvalidateAsync(UserId, cancellationToken);
}
