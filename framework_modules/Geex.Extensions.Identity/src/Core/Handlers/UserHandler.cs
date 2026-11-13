using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Requests.Accounting;
using MediatX;

using Microsoft.AspNetCore.Identity;
using Geex.Extensions.Authentication;

using Volo.Abp;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Identity.Core.Handlers
{
    public class UserHandler :
        IRequestHandler<AssignRoleRequest, bool>,
        IRequestHandler<AssignOrgRequest, bool>,
        IRequestHandler<ChangeOrganizationMembersRequest, bool>,
        IRequestHandler<EditUserRequest, IUser>,
        IRequestHandler<ResetUserPasswordRequest, IUser>,
        IRequestHandler<DeleteUserRequest, bool>,
        IRequestHandler<ChangePasswordRequest, IUser>,
        IRequestHandler<RegisterUserRequest, IUser>,
        IEventHandler<UserOrgChangedEvent>,
        IEventHandler<OrgCodeChangedEvent>,
        ICommonHandler<IUser, User>,
            IRequestHandler<CreateUserRequest, IUser>
    {
        public IUnitOfWork Uow { get; }
        public IUserCreationValidator UserCreationValidator { get; }
        public IPasswordHasher<IUser> PasswordHasher { get; }
        private UserChangeHistory History => Uow.ServiceProvider.GetRequiredService<UserChangeHistory>();
        private DateTimeOffset Now => Uow.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        public UserHandler(IUnitOfWork uow,
            IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher)
        {
            Uow = uow;
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
                var before = History.Capture(user);
                await user.AssignRoles(roles);
                History.Record(user, before, "角色分配");
            }
            return true;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(EditUserRequest request, CancellationToken cancellationToken)
        {
            if (Uow.DbContext.SupportTransaction && !Uow.DbContext.Session.IsInTransaction)
                Uow.DbContext.Session.StartTransaction();
            var user = await Uow.Query<IUser>().OneAsync(request.Id.ToString(), cancellationToken);
            var before = History.Capture(user);
            if (request.OrgCodes != null && Uow.Query<IOrg>().Count(x => request.OrgCodes.Contains(x.Code)) != request.OrgCodes.Distinct().Count())
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "部分组织架构不存在或不可访问, 请刷新后重试.");
            if (request.RoleIds != null && Uow.Query<IRole>().Count(x => request.RoleIds.Contains(x.Id)) != request.RoleIds.Distinct().Count())
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "部分角色不存在或不可访问, 请刷新后重试.");
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

            if (request.IsEnable.HasValue && request.IsEnable.Value != user.IsEnable)
            {
                user.IsEnable = request.IsEnable.Value;
                await user.RevokeSessionsAsync(cancellationToken);
            }

            if (!user.IsEnable) ((User)user).ScheduleDeactivation(null, Now);
            else if (request.DeactivationDate.HasValue) ((User)user).ScheduleDeactivation(request.DeactivationDate.Value, Now);

            if (request.PhoneNumber != default)
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.Username != default)
            {
                user.Username = request.Username;
            }

            if (request.Nickname != default)
            {
                user.Nickname = request.Nickname;
            }

            if (request.RoleIds != null) await user.AssignRoles(request.RoleIds);
            if (request.OrgCodes != null) await user.AssignOrgs(request.OrgCodes);
            History.Record(user, before, "编辑用户");
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
                var before = History.Capture(user);
                var orgs = Uow.Query<IOrg>().Where(x => item.OrgCodes.Contains(x.Code)).ToList();
                await user.AssignOrgs(orgs);
                History.Record(user, before, "组织分配");
            }
            return true;
        }

        public async Task<bool> Handle(ChangeOrganizationMembersRequest request, CancellationToken cancellationToken)
        {
            var ids = request.UserIds.Distinct().ToList();
            if (ids.Count == 0 || ids.Count > 200 || !Uow.Query<IOrg>().Any(x => x.Code == request.OrgCode))
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "请选择有效组织及 1-200 名用户.");
            var users = Uow.Query<IUser>().Where(x => ids.Contains(x.Id)).ToList();
            if (users.Count != ids.Count)
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "部分用户不存在或不可访问, 请刷新后重试.");
            foreach (var user in users)
            {
                var before = History.Capture(user);
                var next = request.Add ? user.OrgCodes.Append(request.OrgCode).Distinct().ToList()
                    : user.OrgCodes.Where(x => x != request.OrgCode).ToList();
                if (user.OrgCodes.SequenceEqual(next)) continue;
                await user.AssignOrgs(next);
                History.Record(user, before, request.Add ? "添加组织成员" : "移出组织成员");
            }
            return true;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(ResetUserPasswordRequest request, CancellationToken cancellationToken)
        {
            var user = Uow.Query<User>().FirstOrDefault(x => request.UserId == x.Id);
            Check.NotNull(user, nameof(user), "用户不存在.");
            user.SetPassword(request.Password);
            await user.RevokeSessionsAsync(cancellationToken);
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
            var user = Uow.Query<User>().GetById(notification.UserId);
            if (user != null)
            {
                await user.InvalidateSessionsCacheAsync(cancellationToken);
            }
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
            var user = Uow.Create(request);
            ((User)user).ScheduleDeactivation(request.DeactivationDate, Now);
            await user.AssignRoles(request.RoleIds);
            await user.AssignOrgs(request.OrgCodes);
            return user;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var currentUser = Uow.GetCurrentUser();
            if (currentUser?.UserId == default)
            {
                return default;
            }
            var user = this.Uow.Query<User>().GetById(currentUser.UserId);
            user.ChangePassword(request.OriginPassword, request.NewPassword);
            await user.RevokeSessionsAsync(cancellationToken);
            return user;
        }



        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            return await this.Uow.Request(new CreateUserRequest
            {
                Username = request.Username,
                IsEnable = true,
                Email = request.Email,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>(),
                AvatarFileId = null,
                Claims = new List<UserClaim>(),
                PhoneNumber = request.PhoneNumber,
                Password = request.Password
            }, cancellationToken);
        }
    }
}
