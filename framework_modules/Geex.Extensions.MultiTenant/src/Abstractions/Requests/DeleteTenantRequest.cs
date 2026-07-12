using Geex.MultiTenant;
using MediatX;

namespace Geex.Extensions.Requests.MultiTenant
{
    public record DeleteTenantRequest(string Code) : IRequest<bool>;
}
