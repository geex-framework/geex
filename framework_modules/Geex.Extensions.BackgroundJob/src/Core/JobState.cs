using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Geex.Extensions.BackgroundJob.Gql.Types;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace Geex.Extensions.BackgroundJob;

[BsonDiscriminator(RootClass = true)]
public class JobState : Entity<JobState>, IJobState
{
    public string JobName { get; set; }
    public string Cron { get; set; }
    public DateTimeOffset? LastExecutionTime { get; set; }
    public DateTimeOffset? NextExecutionTime { get; set; }

    /// <inheritdoc />
    public IQueryable<JobExecutionHistory> ExecutionHistories => LazyQuery(() => ExecutionHistories).OrderByDescending(x => x.ExecutionStartTime);

    public JobState()
    {
        ConfigLazyQuery(x => x.ExecutionHistories, history => history.JobName == JobName, states => history => states.SelectList(x => x.JobName).Contains(history.JobName));
    }

    public class JobStateGqlConfig : GqlConfig.Object<JobState>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<JobState> descriptor)
        {
            descriptor.Field(x=>x.ExecutionHistories)
                .UseOffsetPaging()
                .UseFiltering();
            base.Configure(descriptor);
        }
    }

    public class JobStateBsonConfig : BsonConfig<JobState>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<JobState> map, BsonIndexConfig<JobState> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
        }
    }
}
