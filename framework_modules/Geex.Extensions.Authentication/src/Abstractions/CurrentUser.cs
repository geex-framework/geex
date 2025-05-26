using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Geex.Extensions.Authentication;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;

namespace Geex.Abstractions.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IUnitOfWork _uow;
        private readonly Queue<string?> _parentScopes = new Queue<string?>();
        private string? _userId;
        private IAuthUser? _user;
        public ClaimsIdentity? _claimsIdentity;

        public CurrentUser(IUnitOfWork uow)
        {
            _uow = uow;
        }
        /// <inheritdoc />
        public IAuthUser? User => _user ??= _uow.Query<IAuthUser>().FirstOrDefault(x => x.Id == UserId);
        public string? UserId => _userId ??= _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.FindUserId();

        /// <inheritdoc />
        public ClaimsIdentity? ClaimsIdentity
        {
            get
            {
                if (_claimsIdentity != null)
                {
                    return _claimsIdentity;
                }
                if (!_userId.IsNullOrEmpty())
                {
                    var user = _uow.Query<IAuthUser>().FirstOrDefault(x => x.Id == _userId);
                    var claimsPrincipal = _uow.ServiceProvider.GetService<IUserClaimsPrincipalFactory<IAuthUser>>().CreateAsync(user).ConfigureAwait(true).GetAwaiter().GetResult();
                    _claimsIdentity = claimsPrincipal.Identity as ClaimsIdentity;
                }
                else
                {
                    _claimsIdentity = _uow.ServiceProvider.GetService<ClaimsPrincipal>().Identity as ClaimsIdentity;
                }
                return _claimsIdentity;
            }

        }

        /// <inheritdoc />
        public IDisposable Change(string? userId)
        {
            return SetCurrent(userId);
        }

        private IDisposable SetCurrent(string? userId)
        {
            _parentScopes.Enqueue(UserId);
            _userId = userId;
            _user = null; // Reset user to ensure it is fetched again if needed
            _claimsIdentity = null; // Reset claims identity to ensure it is fetched again if needed
            return new DisposeAction(() =>
            {
                _userId = _parentScopes.Dequeue();
            });
        }
    }
}
