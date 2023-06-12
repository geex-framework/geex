using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Core.Aggregates.Orgs;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs
{
    public class CreateOrgInput : IRequest<Org>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public OrgTypeEnum? OrgType { get; set; } = OrgTypeEnum.Default;
        public string? CreateUserId { get; set; }

        public static CreateOrgInput New(string code, string name, OrgTypeEnum orgType, string? createUserId = default)
        {
            return new CreateOrgInput
            {
                Name = name,
                Code = code,
                OrgType = orgType,
                CreateUserId = createUserId
            };
        }
    }
}