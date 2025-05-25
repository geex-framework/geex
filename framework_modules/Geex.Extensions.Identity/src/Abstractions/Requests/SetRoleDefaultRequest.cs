using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public record SetRoleDefaultRequest : IRequest
    {
        public string RoleId { get; set; }

        public SetRoleDefaultRequest(string roleId)
        {
            RoleId = roleId;
        }
    }
}
