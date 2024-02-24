using Geex.Common.Identity.Api.Aggregates.Roles;
using MediatR;

namespace Geex.Common.Identity.Requests
{
    public class CreateRoleRequest : IRequest<Role>
    {
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsStatic { get; set; }
    }
}