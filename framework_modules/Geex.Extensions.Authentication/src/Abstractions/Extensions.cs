using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.Authentication.Core.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
  public static class Extensions
  {
    public static ICurrentUser? GetCurrentUser(this IUnitOfWork uow)
        => uow.ServiceProvider.GetService<ICurrentUser>();

    public static UserSession? GetSession(this IAuthUser user, LoginProviderEnum provider)
    {
      return user.DbContext.Query<UserSession>()
           .FirstOrDefault(x => x.UserId == user.Id && x.LoginProvider == provider);
    }

    public static async Task<UserSession> BeginSessionAsync(
        this IAuthUser user,
        LoginProviderEnum provider,
        string token,
        CancellationToken cancellationToken = default)
    {
      var session = user.GetSession(provider);
      if (session == null)
      {
        session = user.DbContext.As<IUnitOfWork>().Create(user.Id, provider, token);
      }
      else
      {
        session.Renew(token);
      }

      await session.InvalidateCacheAsync(cancellationToken);
      return session;
    }

    public static LoginProviderEnum GetLoginProvider(this ClaimsIdentity? identity)
    {
      var providerValue = identity?.Claims
          .FirstOrDefault(x => x.Type == GeexClaimType.Provider)?.Value;
      return providerValue.IsNullOrEmpty()
          ? LoginProviderEnum.Local
          : LoginProviderEnum.FromValue(providerValue);
    }
  }
}
