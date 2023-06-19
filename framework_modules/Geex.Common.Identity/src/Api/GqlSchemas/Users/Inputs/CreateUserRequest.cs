using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Api.Aggregates.Users;
using HotChocolate;
using MediatR;

using Volo.Abp;

namespace Geex.Common.Identity.Api.GqlSchemas.Users.Inputs
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
