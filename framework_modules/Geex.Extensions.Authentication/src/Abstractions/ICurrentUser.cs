using System;
using System.Security.Claims;

namespace Geex.Extensions.Authentication;

public interface ICurrentUser
{
    public string? UserId { get; }
    public IAuthUser? User { get; }
    /// <summary>当前 HTTP 认证主体的 Claims，由 JWT/Cookie 解析而来。</summary>
    public ClaimsIdentity ClaimsIdentity { get; }
    /// <summary>服务端会话状态（缓存失效、补充 Claims 版本），与 <see cref="ClaimsIdentity"/> 职责不同。</summary>
    public IUserSession? Session { get; }
    bool IsSuperAdmin => UserId == GeexConstants.SuperAdminId;

    /// <summary>
    /// Change current user, return a disposable object to revert the change
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public IDisposable Change(string? userId);
}
