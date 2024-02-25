using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using MediatR;

namespace Geex.Common.Requests.Identity
{
    public class CreateOrgRequest : IRequest<Org>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public OrgTypeEnum? OrgType { get; set; } = OrgTypeEnum.Default;
        public string? CreateUserId { get; set; }

        public static CreateOrgRequest New(string code, string name, OrgTypeEnum orgType, string? createUserId = default)
        {
            return new CreateOrgRequest
            {
                Name = name,
                Code = code,
                OrgType = orgType,
                CreateUserId = createUserId
            };
        }
    }
}