
using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public record CreateOrgRequest : IRequest<IOrg>
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
