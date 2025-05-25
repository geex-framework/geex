using System.Collections.Generic;
using Geex.Entities;
using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public record EditUserRequest : IRequest<IUser>
    {
        public string Id { get; set; }
        public bool? IsEnable { get; set; }
        public string? Email { get; set; }
        public List<string>? RoleIds { get; set; }
        public List<string>? OrgCodes { get; set; }
        public string? AvatarFileId { get; set; }
        public List<UserClaim>? Claims { get; set; } = new List<UserClaim>();
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
    }
}
