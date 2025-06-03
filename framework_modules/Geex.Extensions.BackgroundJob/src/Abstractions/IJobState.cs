using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Geex.Storage;

namespace Geex.Extensions.BackgroundJob.Gql.Types
{
    public interface IJobState : IEntity
    {
        string JobName { get; }
        DateTimeOffset? LastExecutionTime { get; internal set; }
        IQueryable<JobExecutionHistory> ExecutionHistories { get; }
        public DateTimeOffset? NextExecutionTime { get; internal set; }
    }
}
