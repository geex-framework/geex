using System;
using System.Security.Claims;
using Geex.Extensions.Authentication.Core.Entities;

namespace Geex.Extensions.Authentication;

public interface ICurrentUser
{
    public string? UserId { get; }
    public IAuthUser? User { get; }
    /// <summary>当前 HTTP 认证主体的 Claims，由 JWT/Cookie 解析而来。</summary>
    public ClaimsIdentity ClaimsIdentity { get; }
    /// <summary>当前登录渠道对应的服务端会话实体。</summary>
    public UserSession? Session { get; }
    bool IsSuperAdmin => UserId == GeexConstants.SuperAdminId;

    /// <summary>
    /// Change current user, return a disposable object to revert the change
    /// </summary>
    public IDisposable Change(string? userId);
}
