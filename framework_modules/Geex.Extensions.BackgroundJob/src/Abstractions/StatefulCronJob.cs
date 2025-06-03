using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.BackgroundJob.Core;
using Geex.Extensions.BackgroundJob.Gql.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.BackgroundJob
{
    public abstract class StatefulCronJob<TState, TImplementation> : CronJob<TImplementation> where TImplementation : class, IHostedService where TState : JobState, new()
    {
        /// <inheritdoc />
        protected StatefulCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {

        }

        /// <inheritdoc />
        public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var startTime = DateTimeOffset.Now;
            var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
            var jobName = this.GetType().Name;
            try
            {
                var jobState = uow.Query<TState>().FirstOrDefault(x => x.JobName == jobName)
                               ?? uow.Attach(new TState
                               {
                                   JobName = jobName,
                                   Cron = Cron.ToString()
                               });

                await this.Run(scope.ServiceProvider, jobState, cancellationToken);
                var endTime = DateTimeOffset.Now;
                var executionHistory = new JobExecutionHistory
                {
                    JobName = jobName,
                    ExecutionStartTime = startTime,
                    IsSuccess = true,
                    ExecutionEndTime = endTime,
                };
                uow.Attach(executionHistory);
                jobState.LastExecutionTime = endTime;
                jobState.NextExecutionTime = Cron.GetNextOccurrence(endTime, TimeZoneInfo.Utc);
                await uow.SaveChanges(cancellationToken);
            }
            catch (Exception e)
            {
                var endTime = DateTimeOffset.Now;
                var jobState = uow.Query<TState>().FirstOrDefault(x => x.JobName == jobName)
                               ?? uow.Attach(new TState
                               {
                                   JobName = jobName,
                                   Cron = Cron.ToString(),
                                   LastExecutionTime = endTime,
                                   NextExecutionTime = Cron.GetNextOccurrence(endTime, TimeZoneInfo.Utc)
                               });

                var executionHistory = new JobExecutionHistory
                {
                    JobName = jobName,
                    ExecutionStartTime = startTime,
                    IsSuccess = false,
                    Message = e.ToString(),
                    ExecutionEndTime = endTime
                };
                uow.Attach(executionHistory);
                await uow.SaveChanges(cancellationToken);
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }

        public abstract Task Run(IServiceProvider serviceProvider, TState jobState, CancellationToken cancellationToken);
    }
}
