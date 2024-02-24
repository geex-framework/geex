using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Identity.Requests
{
    public record AssignRoleRequest : IRequest
    {
        public List<string> UserIds { get; set; }
        public List<string> Roles { get; set; }
    }
}