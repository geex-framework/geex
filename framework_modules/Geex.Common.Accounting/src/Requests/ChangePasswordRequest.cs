using MediatR;

namespace Geex.Common.Accounting.Requests
{
    public class ChangePasswordRequest : IRequest
    {
        /// <summary>
        /// 原密码
        /// </summary>
        public string OriginPassword { get; set; }
        /// <summary>
        /// 新密码(建议前端二次确认)
        /// </summary>
        public string NewPassword { get; set; }
    }
}
