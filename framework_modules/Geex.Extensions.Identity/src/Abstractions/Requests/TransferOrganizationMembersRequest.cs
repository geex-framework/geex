using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity.Requests;

public sealed record TransferOrganizationMembersRequest : IRequest<bool>
{
    public string SourceOrgCode { get; set; } = "";
    public string TargetOrgCode { get; set; } = "";
    public List<string> UserIds { get; set; } = new();
}
