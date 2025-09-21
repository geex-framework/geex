using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record CopyRoleRequest : IRequest<IRole>
    {
        public string FromRoleCode { get; set; }
        public string? ToRoleCode { get; set; }
        public string? ToRoleName { get; set; }

        public static CopyRoleRequest New(string fromRoleCode, string? toRoleCode = null, string? toRoleName = null)
        {
            return new CopyRoleRequest
            {
                FromRoleCode = fromRoleCode,
                ToRoleCode = toRoleCode,
                ToRoleName = toRoleName
            };
        }
    }
}
