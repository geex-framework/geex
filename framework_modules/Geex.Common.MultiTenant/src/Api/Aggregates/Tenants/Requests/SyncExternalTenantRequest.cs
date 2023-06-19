using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.MultiTenant;

using MediatR;

namespace Geex.Common.MultiTenant.Api.Aggregates.Tenants.Requests
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
