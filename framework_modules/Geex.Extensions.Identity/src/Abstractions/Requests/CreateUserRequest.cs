using System.Collections.Generic;
using Geex.Abstractions;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public interface ICreateUserRequest
    {
        string Username { get; set; }
        bool IsEnable { get; set; }
        string? Email { get; set; }
        List<string>? RoleIds { get; set; }
        List<string>? OrgCodes { get; set; }
        string? AvatarFileId { get; set; }
        List<UserClaim>? Claims { get; set; }
        string? PhoneNumber { get; set; }
        string? Password { get; set; }
        string? Nickname { get; set; }
        string? OpenId { get; set; }
        LoginProviderEnum? Provider { get; set; }
    }

    public record CreateUserRequest : CreateUserRequest<User>
    {

    }
    public record CreateUserRequest<TUser> : IRequest<IUser>, ICreateUserRequest
    {
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
