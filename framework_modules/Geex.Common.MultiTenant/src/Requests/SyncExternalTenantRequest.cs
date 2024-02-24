using Geex.Common.Abstraction.MultiTenant;
using MediatR;

namespace Geex.Common.MultiTenant.Requests
{
    public class SyncExternalTenantRequest : IRequest<ITenant>
    {
        public SyncExternalTenantRequest(string code)
        {
            Code = code;
        }

        public string Code { get; set; }
    }
}
