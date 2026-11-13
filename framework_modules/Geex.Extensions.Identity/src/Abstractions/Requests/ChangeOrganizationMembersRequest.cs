using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity.Requests;

public sealed record ChangeOrganizationMembersRequest : IRequest<bool>
{
    public string OrgCode { get; set; } = "";
    public List<string> UserIds { get; set; } = new();
    public bool Add { get; set; }
}
