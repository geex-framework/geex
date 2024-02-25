using System.Collections.Generic;
using Geex.Common.Authorization;
using Geex.Common.Authorization.GqlSchema.Types;
using MediatR;

namespace Geex.Common.Requests.Authorization
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