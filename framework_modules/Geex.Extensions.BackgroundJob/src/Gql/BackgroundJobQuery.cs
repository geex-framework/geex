using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.BackgroundJob.Gql.Requests;
using Geex.Extensions.BackgroundJob.Gql.Types;
using Geex.Gql.Types;
using HotChocolate.Types;
using HotChocolate.Data;
using MongoDB.Driver.Linq;

namespace Geex.Extensions.BackgroundJob.Gql
{
    public sealed class BackgroundJobQuery : QueryExtension<BackgroundJobQuery>
    {
        private readonly IUnitOfWork _uow;

        public BackgroundJobQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<BackgroundJobQuery> descriptor)
        {
            descriptor.Field(x => x.JobState(default))
            .UseOffsetPaging<ObjectType<JobState>>()
            .UseFiltering<JobState>()
            .UseSorting<JobState>();
            base.Configure(descriptor);
        }

        /// <summary>
        /// Query job states with filtering and pagination
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IQueryable<IJobState>> JobState(QueryJobStatesRequest? request)
        {
            request ??= new QueryJobStatesRequest();
            return _uow.Request(request);
        }
    }
}
