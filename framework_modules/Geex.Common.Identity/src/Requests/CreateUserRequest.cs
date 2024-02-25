using System.Collections.Generic;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using MediatR;

namespace Geex.Common.Requests.Identity
{
    public class CreateUserRequest : IRequest<IUser>
    {
        public CreateUserRequest()
        {

        }

        public string Username { get; set; }

        public bool IsEnable { get; set; } = true;
        public string? Email { get; set; }
        public List<string>? RoleIds { get; set; } = new List<string>();
        public List<string>? OrgCodes { get; set; } = new List<string>();
        public string? AvatarFileId { get; set; }
        public List<UserClaim>? Claims { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? Nickname { get; set; } = "";
        public string? OpenId { get; set; } = "";
        public LoginProviderEnum? Provider { get; set; } = LoginProviderEnum.Local;
    }
}
