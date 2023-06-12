using System.Collections.Generic;

using MediatR;

using MongoDB.Bson;

namespace Geex.Common.Identity.Api.GqlSchemas.Users.Inputs
{
    public record AssignOrgRequest : IRequest<Unit>
    {
        public List<UserOrgMapItem> UserOrgsMap { get; set; }
    }

    public record UserOrgMapItem
    {
        public string UserId { get; set; }
        public List<string> OrgCodes { get; set; }
    }
}