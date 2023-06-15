
using MediatR;

namespace _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs
{
    public class Edit_aggregate_Request : IRequest<Unit>
    {
        public string Id { get; set; }
        public string? Name { get; set; }
    }
}