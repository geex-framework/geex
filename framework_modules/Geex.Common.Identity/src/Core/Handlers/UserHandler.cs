using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Events;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;
using Geex.Common.Identity.Requests;
using Geex.Common.Requests.Identity;

using HotChocolate.Utilities;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

using Volo.Abp;

using Role = Geex.Common.Identity.Api.Aggregates.Roles.Role;

namespace Geex.Common.Identity.Core.Handlers
{
    public class UserHandler :
        IRequestHandler<AssignRoleRequest, bool>,
        IRequestHandler<AssignOrgRequest, bool>,
        IRequestHandler<EditUserRequest, IUser>,
        IRequestHandler<ResetUserPasswordRequest, IUser>,
        IRequestHandler<DeleteUserRequest, bool>,
        INotificationHandler<UserOrgChangedEvent>,
        INotificationHandler<OrgCodeChangedEvent>,
        ICommonHandler<IUser, User>,
            IRequestHandler<CreateUserRequest, IUser>
    {
        private IRedisDatabase _redis;
        public IUnitOfWork Uow { get; }
        public IUserCreationValidator UserCreationValidator { get; }
        public IPasswordHasher<IUser> PasswordHasher { get; }
        public UserHandler(IUnitOfWork uow,
            IRedisDatabase redis, IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher)
        {
            Uow = uow;
            _redis = redis;
            UserCreationValidator = userCreationValidator;
            PasswordHasher = passwordHasher;
        }
        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<bool> Handle(AssignRoleRequest request, CancellationToken cancellationToken)
        {
            var users = await Task.FromResult(Uow.Query<IUser>().Where(x => request.UserIds.Contains(x.Id)).ToList());
            var roles = await Task.FromResult(Uow.Query<IRole>().Where(x => request.Roles.Contains(x.Id)).ToList());
            foreach (var user in users)
            {
                await user.AssignRoles(roles);
            }
            return true;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(EditUserRequest request, CancellationToken cancellationToken)
        {
            var user = await Uow.Query<IUser>().OneAsync(request.Id.ToString(), cancellationToken);
            if (request.Claims != default)
            {
                user.Claims = request.Claims;
            }

            if (request.AvatarFileId != default)
            {
                user.AvatarFileId = request.AvatarFileId;
            }

            if (request.Email != default)
            {
                user.Email = request.Email;
            }

            if (request.IsEnable != default)
            {
                user.IsEnable = request.IsEnable.Value;
            }

            if (request.OrgCodes != default)
            {
                user.OrgCodes = request.OrgCodes;
            }

            if (request.PhoneNumber != default)
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.Username != default)
            {
                user.Username = request.Username;
            }

            if (request.RoleIds != null) await user.AssignRoles(request.RoleIds);
            if (request.OrgCodes != null) await user.AssignOrgs(request.OrgCodes);
            return user;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<bool> Handle(AssignOrgRequest request, CancellationToken cancellationToken)
        {
            foreach (var item in request.UserOrgsMap)
            {
                var user = await Uow.Query<IUser>().OneAsync(item.UserId, cancellationToken);
                var orgs = Uow.Query<IOrg>().Where(x => item.OrgCodes.Contains(x.Code)).ToList();
                await user.AssignOrgs(orgs);
            }
            return true;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(ResetUserPasswordRequest request, CancellationToken cancellationToken)
        {
            var user = Uow.Query<IUser>().FirstOrDefault(x => request.UserId == x.Id);
            Check.NotNull(user, nameof(user), "用户不存在.");
            user.SetPassword(request.Password);
            return user;
        }
        /// <summary>
        /// 用户组织架构更新后, 需要更新用户claim
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual async Task Handle(UserOrgChangedEvent notification, CancellationToken cancellationToken)
        {
            // 用户组织架构变化量通常比较大, 这里直接FireAndForget不等待
            await this._redis.RemoveNamedAsync<UserSessionCache>(notification.UserId, command: CommandFlags.FireAndForget);
        }

        /// <summary>
        /// 组织架构更新后更新所有用户已拥有的组织架构信息
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task Handle(OrgCodeChangedEvent notification, CancellationToken cancellationToken)
        {
            var users = Uow.Query<IUser>().Where(x => x.OrgCodes.Contains(notification.OldOrgCode)).ToList();
            foreach (var user in users)
            {
                // 替换被修改的orgCode
                user.OrgCodes.ReplaceOne(code => code == notification.OldOrgCode, notification.NewOrgCode);
            }
        }

        /// <inheritdoc />
        public async Task<bool> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
        {
            var user = Uow.Query<IUser>().GetById(request.Id);
            await user.DeleteAsync();
            return true;
        }

        /// <inheritdoc />
        public virtual async Task<IUser> Handle(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = new User(this.UserCreationValidator, this.PasswordHasher, request);
            Uow.Attach(user);
            await user.AssignRoles(request.RoleIds);
            await user.AssignOrgs(request.OrgCodes);
            await user.SaveAsync(cancellationToken);
            return user;
        }
    }
}
