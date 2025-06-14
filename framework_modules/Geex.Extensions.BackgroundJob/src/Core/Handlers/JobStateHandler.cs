using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.BackgroundJob.Gql.Requests;
using Geex.Extensions.BackgroundJob.Gql.Types;

using MediatX;

namespace Geex.Extensions.BackgroundJob.Core.Handlers
{
    public class JobStateHandler : IRequestHandler<QueryJobStatesRequest, IQueryable<IJobState>>
    {
        private IUnitOfWork _uow;

        public JobStateHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <inheritdoc />
        public async Task<IQueryable<IJobState>> Handle(QueryJobStatesRequest request, CancellationToken cancellationToken)
        {
            var result = _uow.Query<IJobState>();
            if (!string.IsNullOrEmpty(request.JobName))
            {
                result = result.Where(x=>x.JobName == request.JobName);
            }
            return result;
        }
    }
}
