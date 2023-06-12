using Geex.Common.Identity.Api.Aggregates.Roles;
using HotChocolate;
using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs
{
    public class CreateRoleInput : IRequest<Role>
    {
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsStatic { get; set; }
    }
}