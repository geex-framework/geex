using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Geex.Abstractions.Entities;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Abstractions.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IUnitOfWork _uow;
        private readonly Queue<string?> _parentScopes = new Queue<string?>();
        private string? _userId;
        private IUser? _user;

        public CurrentUser(IUnitOfWork uow)
        {
            _uow = uow;
        }
        /// <inheritdoc />
        public IUser? User => _user ??= _uow.Query<IUser>().FirstOrDefault(x => x.Id == UserId);
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
            _user = _uow.Query<IUser>().FirstOrDefault(x => x.Id == userId);
            return new DisposeAction(() =>
            {
                _userId = _parentScopes.Dequeue();
            });
        }
    }

    public interface ICurrentUser
    {
        public IUser? User { get; }
        public string? UserId { get; }
        bool IsSuperAdmin => UserId == IUser.SuperAdminId;

        /// <summary>
        /// Change current user, return a disposable object to revert the change
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IDisposable Change(string? userId);
    }
}
