using System.Linq;
using Geex.Extensions.BackgroundJob.Gql.Types;

using MediatX;

namespace Geex.Extensions.BackgroundJob.Gql.Requests
{
    public class QueryJobExecutionHistoryRequest : IRequest<IQueryable<JobExecutionHistory>>
    {
        public string JobName { get; set; }
    }
}
