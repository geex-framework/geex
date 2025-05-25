using System;

namespace Geex.Abstractions.Authentication;

public interface ICurrentUser
{
    public string? UserId { get; }
    bool IsSuperAdmin => UserId == GeexConstants.SuperAdminId;

    /// <summary>
    /// Change current user, return a disposable object to revert the change
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public IDisposable Change(string? userId);
}