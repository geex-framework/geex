using System.Linq;
using Geex.Extensions.BackgroundJob.Gql.Types;

using MediatX;

namespace Geex.Extensions.BackgroundJob.Gql.Requests
{
    public class QueryJobStatesRequest : IRequest<IQueryable<IJobState>>
    {
        public string JobName { get; set; }
    }
}
