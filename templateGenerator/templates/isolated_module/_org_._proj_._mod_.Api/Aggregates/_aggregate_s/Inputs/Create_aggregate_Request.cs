using MediatR;

namespace _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs
{
    public class Create_aggregate_Request : IRequest<I_aggregate_>
    {
        public string Name { get; set; }
    }
}
