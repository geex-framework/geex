﻿using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Authorization.Requests
{
    public record UserRoleChangeRequest : IRequest
    {
        public UserRoleChangeRequest(string UserId, List<string> RoleIds)
        {
            this.UserId = UserId;
            this.RoleIds = RoleIds;
        }

        public string UserId { get; init; }
        public List<string> RoleIds { get; init; }

        public void Deconstruct(out string UserId, out List<string> RoleIds)
        {
            UserId = this.UserId;
            RoleIds = this.RoleIds;
        }
    }
}
