using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IUnitOfWork _uow;
        private readonly Queue<string?> _parentScopes = new Queue<string?>();
        private string? _userId;
        private IAuthUser? _user;
        public ClaimsIdentity? _claimsIdentity;
        private IUserSession? _session;

        public CurrentUser(IUnitOfWork uow)
        {
            _uow = uow;
        }
        /// <inheritdoc />
        public IAuthUser? User => _user ??= _uow.Query<IAuthUser>().FirstOrDefault(x => x.Id == UserId);
        public string? UserId => _userId ??= _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.FindUserId();

        /// <inheritdoc />
        public ClaimsIdentity? ClaimsIdentity => _claimsIdentity ??= _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.Identity as ClaimsIdentity;

        /// <inheritdoc />
        public IUserSession? Session => UserId is { } userId ? _session ??= _uow.GetUserSession(userId) : null;

        /// <inheritdoc />
        public IDisposable Change(string? userId)
        {
            return SetCurrent(userId);
        }

        private IDisposable SetCurrent(string? userId)
        {
            _parentScopes.Enqueue(UserId);
            _userId = userId;
            _user = null;
            _claimsIdentity = null;
            _session = null;
            return new DisposeAction(() =>
            {
                _userId = _parentScopes.Dequeue();
                _session = null;
            });
        }
    }
}
