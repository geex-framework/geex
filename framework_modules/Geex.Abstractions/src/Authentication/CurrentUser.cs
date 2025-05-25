using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;

namespace Geex.Abstractions.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IUnitOfWork _uow;
        private readonly Queue<string?> _parentScopes = new Queue<string?>();
        private string? _userId;

        public CurrentUser(IUnitOfWork uow)
        {
            _uow = uow;
        }
        /// <inheritdoc />
        public string? UserId => _userId ?? _uow.ServiceProvider.GetService<ClaimsPrincipal>()?.FindUserId();

        /// <inheritdoc />
        public IDisposable Change(string? userId)
        {
            return SetCurrent(userId);
        }

        private IDisposable SetCurrent(string? userId)
        {
            _parentScopes.Enqueue(UserId);
            _userId = userId;
            return new DisposeAction(() =>
            {
                _userId = _parentScopes.Dequeue();
            });
        }
    }
}
