using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Api.Aggregates.Roles;
using MediatR;

namespace Geex.Common.Requests.Identity
{
    public record CreateRoleRequest : IRequest<IRole>
    {
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsStatic { get; set; }
    }
}