﻿using MediatR;

namespace Geex.Common.Requests.Identity
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
