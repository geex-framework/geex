﻿using System.Collections.Generic;
using Geex.Common.Authorization.GqlSchema.Types;
using MediatR;

using MongoDB.Bson;

namespace Geex.Common.Authorization.GqlSchema.Inputs
{
    public record AuthorizeInput : IRequest<Unit>
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