using MediatR;

namespace Geex.Common.Identity.Requests
{
    public class SetRoleDefaultRequest : IRequest
    {
        public string RoleId { get; set; }

        public SetRoleDefaultRequest(string roleId)
        {
            RoleId = roleId;
        }
    }
}
