
using System.Collections.Generic;
using MediatR;

namespace _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs
{
    public class Delete_aggregate_Request : IRequest<Unit>
    {
        public List<string> Ids { get; set; }
    }
}