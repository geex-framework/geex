using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Geex.Extensions.Authentication.Core.Entities;
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
        private UserSession? _session;

        public CurrentUser(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IAuthUser? User => _user ??= _uow.Query<IAuthUser>().FirstOrDefault(x => x.Id == UserId);
        public string? UserId => _userId ??= _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.FindUserId();

        public ClaimsIdentity? ClaimsIdentity => _claimsIdentity ??= _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.Identity as ClaimsIdentity;

        public UserSession? Session
        {
            get
            {
                if (UserId.IsNullOrEmpty())
                {
                    return null;
                }

                var provider = ClaimsIdentity.GetLoginProvider();
                if (_session != null)
                {
                    return _session;
                }

                var session = _uow.Query<UserSession>()
                    .FirstOrDefault(x => x.UserId == UserId && x.LoginProvider == provider);
                return _session = session == null ? null : _uow.Attach(session);
            }
        }

        public IDisposable Change(string? userId) => SetCurrent(userId);

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
