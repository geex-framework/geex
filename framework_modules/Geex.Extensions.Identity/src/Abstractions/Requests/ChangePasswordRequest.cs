
using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public record ChangePasswordRequest : IRequest<IUser>
    {
        /// <summary>
        /// 原密码
        /// 注：此处的 Password 应是经过前端哈希处理后的密码
        /// </summary>
        public string OriginPassword { get; set; }
        /// <summary>
        /// 新密码(建议前端二次确认)
        /// 注：此处的 Password 应是经过前端哈希处理后的密码
        /// </summary>
        public string NewPassword { get; set; }
    }
}
