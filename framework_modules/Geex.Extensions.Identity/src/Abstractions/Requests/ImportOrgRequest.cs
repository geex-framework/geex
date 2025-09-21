using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record ImportOrgRequest : IRequest<IEnumerable<IOrg>>
    {
        public IEnumerable<ImportOrgItem> OrgItems { get; set; }

        public static ImportOrgRequest New(IEnumerable<ImportOrgItem> orgItems)
        {
            return new ImportOrgRequest
            {
                OrgItems = orgItems
            };
        }
    }

    public record ImportOrgItem
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public OrgTypeEnum OrgType { get; set; } = OrgTypeEnum.Default;
        public string? ParentOrgCode { get; set; }
    }
}
