using Geex.Abstractions.Entities;
using MediatR;

namespace Geex.Common.Identity.Requests
{
    public record CreateRoleRequest : IRequest<IRole>
    {
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsStatic { get; set; }
    }
}
