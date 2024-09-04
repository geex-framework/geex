using System.Collections.Generic;
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
    public class AccountHandler : IRequestHandler<ChangePasswordRequest>,
        IRequestHandler<RegisterUserRequest>

    {
        private IMediator _mediator;

        public AccountHandler(IMediator mediator, ICurrentUser currentUser)
        {
            _mediator = mediator;
            this.CurrentUser = currentUser;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task Handle(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var query = await this._mediator.Send(new QueryRequest<IUser>(x => x.Id == CurrentUser.UserId), cancellationToken);
            var user = query.First();
            user.ChangePassword(request.OriginPassword, request.NewPassword);
            return;
        }



        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task Handle(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            await this._mediator.Send(new CreateUserRequest
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
