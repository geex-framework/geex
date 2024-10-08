﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Requests;
using Geex.Common.Abstractions;
using Geex.Common.Requests.Accounting;
using Geex.Common.Requests.Identity;
using MediatR;

namespace Geex.Common.Accounting.Handlers
{
    public class AccountHandler : IRequestHandler<ChangePasswordRequest, IUser>,
        IRequestHandler<RegisterUserRequest>

    {
        private IUnitOfWork _uow;

        public AccountHandler(IUnitOfWork uow, ICurrentUser currentUser)
        {
            _uow = uow;
            this.CurrentUser = currentUser;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IUser> Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            if (CurrentUser?.UserId == default)
            {
                return default;
            }
            var user = this._uow.Query<IUser>().GetById(CurrentUser.UserId);
            user.ChangePassword(request.OriginPassword, request.NewPassword);
            return user;
        }



        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task Handle(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            await this._uow.Request(new CreateUserRequest
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
            return;
        }

        public ICurrentUser CurrentUser { get; }
    }

}
