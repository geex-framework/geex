using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record UpdateOrgRequest : IRequest<IOrg>
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public OrgTypeEnum? OrgType { get; set; }

        public static UpdateOrgRequest New(string id, string? name = null, string? code = null, OrgTypeEnum? orgType = null)
        {
            return new UpdateOrgRequest
            {
                Id = id,
                Name = name,
                Code = code,
                OrgType = orgType
            };
        }
    }
}
