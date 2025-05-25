using Geex.MultiTenant;
using MediatR;

namespace Geex.Extensions.Requests.MultiTenant
{
    public record SyncExternalTenantRequest : IRequest<ITenant>
    {
        public SyncExternalTenantRequest(string code)
        {
            Code = code;
        }

        public string Code { get; set; }
    }
}
