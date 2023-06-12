using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs
{
    public class SetRoleDefaultInput : IRequest<Unit>
    {
        public string RoleId { get; set; }

        public SetRoleDefaultInput(string roleId)
        {
            RoleId = roleId;
        }
    }
}
