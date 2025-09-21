using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record MoveOrgRequest : IRequest<bool>
    {
        public string Id { get; set; }
        public string NewParentOrgCode { get; set; }

        public static MoveOrgRequest New(string id, string newParentOrgCode)
        {
            return new MoveOrgRequest
            {
                Id = id,
                NewParentOrgCode = newParentOrgCode
            };
        }
    }
}
