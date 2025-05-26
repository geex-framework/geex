using System;
using System.Security.Claims;
using Geex.Extensions.Authentication;

namespace Geex.Abstractions.Authentication;

public interface ICurrentUser
{
    public string? UserId { get; }
    public IAuthUser? User { get; }
    public ClaimsIdentity ClaimsIdentity { get; }
    bool IsSuperAdmin => UserId == GeexConstants.SuperAdminId;

    /// <summary>
    /// Change current user, return a disposable object to revert the change
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public IDisposable Change(string? userId);
}