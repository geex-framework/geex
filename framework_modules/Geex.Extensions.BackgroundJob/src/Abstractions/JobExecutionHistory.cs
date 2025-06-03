using System;

using Geex.Storage;

using MongoDB.Bson.Serialization;

namespace Geex.Extensions.BackgroundJob.Gql.Types;

public class JobExecutionHistory : Entity<JobExecutionHistory>
{
    public string JobName { get; set; }
    public DateTimeOffset ExecutionStartTime { get; set; }
    public DateTimeOffset? ExecutionEndTime { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public class JobExecutionHistoryGqlConfig : GqlConfig.Object<JobExecutionHistory>
    {

    }

    public class JobExecutionHistoryBsonConfig : BsonConfig<JobExecutionHistory>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<JobExecutionHistory> map, BsonIndexConfig<JobExecutionHistory> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.ExecutionStartTime));
            indexConfig.MapIndex(x => x.Descending(o => o.ExecutionEndTime));
            indexConfig.MapIndex(x => x.Hashed(o => o.JobName));
        }
    }
}