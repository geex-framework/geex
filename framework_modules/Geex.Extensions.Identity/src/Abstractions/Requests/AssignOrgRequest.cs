using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record AssignOrgRequest : IRequest<bool>
    {
        public List<UserOrgMapItem> UserOrgsMap { get; set; }
    }

    public record UserOrgMapItem
    {
        public string UserId { get; set; }
        public List<string> OrgCodes { get; set; }
    }
}
