using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record AssignRoleRequest : IRequest<bool>
    {
        public List<string> UserIds { get; set; }
        public List<string> Roles { get; set; }
    }
}
