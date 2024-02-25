using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Requests.Identity
{
    public record AssignOrgRequest : IRequest
    {
        public List<UserOrgMapItem> UserOrgsMap { get; set; }
    }

    public record UserOrgMapItem
    {
        public string UserId { get; set; }
        public List<string> OrgCodes { get; set; }
    }
}