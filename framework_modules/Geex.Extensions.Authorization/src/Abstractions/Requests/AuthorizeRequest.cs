﻿using System.Collections.Generic;
using Geex.Extensions.Authorization.Gql.Types;
using MediatX;

namespace Geex.Extensions.Authorization.Requests
{
    public record AuthorizeRequest : IRequest
    {
        public AuthorizeTargetType AuthorizeTargetType { get; set; }
        public List<AppPermission> AllowedPermissions { get; set; }
        /// <summary>
        /// 授权目标:
        /// 用户or角色id
        /// </summary>
        public string Target { get; set; }
    }
}
