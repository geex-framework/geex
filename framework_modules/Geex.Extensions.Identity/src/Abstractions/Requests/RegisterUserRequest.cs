using Geex.Extensions.Identity;

using MediatX;

// ReSharper disable once CheckNamespace
namespace Geex.Extensions.Requests.Accounting
{
    public record RegisterUserRequest : IRequest<IUser>
    {
        /// <summary>
        /// 注：此处的 Password 应是经过前端哈希处理后的密码
        /// </summary>
        public string Password { get; set; }
        public string Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
