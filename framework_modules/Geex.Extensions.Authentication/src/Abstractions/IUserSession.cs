using System;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Authentication;

/// <summary>
/// 服务端会话状态（缓存失效、补充 Claims 版本）。
/// 与 <see cref="ICurrentUser.ClaimsIdentity"/> 不同：后者反映当前 HTTP 认证主体的 Claims，本接口管理 Redis 侧会话元数据。
/// </summary>
public interface IUserSession
{
    string UserId { get; }

    Task<DateTimeOffset> GetLastUpdatedOnAsync(CancellationToken cancellationToken = default);

    Task<UserSession> BeginAsync(LoginProviderEnum provider, string token, CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
