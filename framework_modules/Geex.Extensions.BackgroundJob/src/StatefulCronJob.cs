﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace Geex.Extensions.BackgroundJob
{
    [BsonDiscriminator(RootClass = true)]
    public class JobState : EntityBase<JobState>
    {
        public string JobName { get; set; }
    }
    public abstract class StatefulCronJob<TState, TImplementation> : CronJob<TImplementation> where TImplementation : class, IHostedService where TState : JobState, new()
    {
        /// <inheritdoc />
        protected StatefulCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {

        }

        /// <inheritdoc />
        public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var uow = serviceProvider.GetService<IUnitOfWork>();
            var existedJobState = uow.Query<TState>().FirstOrDefault(x => x.JobName == this.GetType().Name);
            var jobState = existedJobState ?? uow.Attach(new TState
            {
                JobName = typeof(TImplementation).Name
            });

            await this.Run(serviceProvider, jobState, cancellationToken);
        }

        public abstract Task Run(IServiceProvider serviceProvider, TState jobState, CancellationToken cancellationToken);
    }
}
