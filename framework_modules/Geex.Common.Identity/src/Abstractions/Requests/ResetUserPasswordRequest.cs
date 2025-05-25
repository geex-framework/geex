using Geex.Abstractions.Entities;
using MediatR;

namespace Geex.Common.Identity.Requests
{
    public record ResetUserPasswordRequest : IRequest<IUser>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// 新密码
        /// </summary>
        public string Password { get; set; }
    }
}
