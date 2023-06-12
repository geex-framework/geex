using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Api.GqlSchemas.Users.Inputs;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;

using HotChocolate.Subscriptions;

using Mediator;

using MediatR;

using Microsoft.AspNetCore.Identity;

using MongoDB.Bson;
using MongoDB.Entities;

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

using Volo.Abp;

using Role = Geex.Common.Identity.Api.Aggregates.Roles.Role;

namespace Geex.Common.Identity.Core.Handlers
{
    public class UserHandler :
        IRequestHandler<AssignRoleRequest, Unit>,
        IRequestHandler<AssignOrgRequest, Unit>,
        IRequestHandler<CreateUserRequest, IUser>,
        IRequestHandler<EditUserRequest, Unit>,
        IRequestHandler<ResetUserPasswordRequest>,
        INotificationHandler<UserOrgChangedEvent>,
        INotificationHandler<OrgCodeChangedEvent>,
        ICommonHandler<IUser, User>
    {
        private IRedisDatabase _redis;
        public DbContext DbContext { get; }
        public IUserCreationValidator UserCreationValidator { get; }
        public IPasswordHasher<IUser> PasswordHasher { get; }

        public UserHandler(DbContext dbContext,
         IUserCreationValidator userCreationValidator,
            IPasswordHasher<IUser> passwordHasher, IRedisDatabase redis)
        {
            DbContext = dbContext;
            UserCreationValidator = userCreationValidator;
            PasswordHasher = passwordHasher;
            _redis = redis;
        }
        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Unit> Handle(AssignRoleRequest request, CancellationToken cancellationToken)
        {
            var users = await Task.FromResult(DbContext.Queryable<User>().Where(x => request.UserIds.Contains(x.Id)).ToList());
            var roles = await Task.FromResult(DbContext.Queryable<Role>().Where(x => request.Roles.Contains(x.Id)).ToList());
            foreach (var user in users)
            {
                await user.AssignRoles(roles);
            }
            return Unit.Value;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Unit> Handle(EditUserRequest request, CancellationToken cancellationToken)
        {
            var user = await DbContext.Queryable<User>().OneAsync(request.Id.ToString(), cancellationToken);
            if (request.Claims.HasValue)
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

            await user.AssignRoles(request.RoleIds);
            await user.AssignOrgs(request.OrgCodes);
            return Unit.Value;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<IUser> Handle(CreateUserRequest request, CancellationToken cancellationToken)
        {
            User user;
            if (request.OpenId.IsNullOrEmpty())
            {
                user = User.New(this.UserCreationValidator, this.PasswordHasher, request.Username, request.Username, request.PhoneNumber, request.Email, request.Password);
            }
            else
            {
                user = User.NewExternal(this.UserCreationValidator, this.PasswordHasher, request.OpenId, request.Provider, request.Username, request.PhoneNumber, request.Email, request.Password);
            }

            DbContext.Attach(user);
            user.Nickname = request.Nickname;
            user.AvatarFileId = request.AvatarFileId;
            user.IsEnable = request.IsEnable;
            await user.AssignRoles(request.RoleIds);
            await user.AssignOrgs(request.OrgCodes);
            await user.SaveAsync(cancellationToken);
            return user;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Unit> Handle(AssignOrgRequest request, CancellationToken cancellationToken)
        {
            foreach (var item in request.UserOrgsMap)
            {
                var user = await DbContext.Queryable<User>().OneAsync(item.UserId, cancellationToken);
                var orgs = DbContext.Queryable<Org>().Where(x => item.OrgCodes.Contains(x.Code)).ToList();
                await user.AssignOrgs(orgs);
            }


            return Unit.Value;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<Unit> Handle(ResetUserPasswordRequest request, CancellationToken cancellationToken)
        {
            var user = DbContext.Queryable<User>().FirstOrDefault(x => request.UserId == x.Id);
            Check.NotNull(user, nameof(user), "用户不存在.");
            user.SetPassword(request.Password);
            return Unit.Value;
        }
        /// <summary>
        /// 用户组织架构更新后, 需要更新用户claim
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task Handle(UserOrgChangedEvent notification, CancellationToken cancellationToken)
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
        public async Task Handle(OrgCodeChangedEvent notification, CancellationToken cancellationToken)
        {
            var users = DbContext.Queryable<User>().Where(x => x.OrgCodes.Contains(notification.OldOrgCode)).ToList();
            foreach (var user in users)
            {
                // 替换被修改的orgCode
                user.OrgCodes.ReplaceOne(code => code == notification.OldOrgCode, notification.NewOrgCode);
            }
        }
    }
}
