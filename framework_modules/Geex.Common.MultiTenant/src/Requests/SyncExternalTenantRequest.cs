using Geex.Abstractions.MultiTenant;
using MediatR;

namespace Geex.Common.Requests.MultiTenant
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
