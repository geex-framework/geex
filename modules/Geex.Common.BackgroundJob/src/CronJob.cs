﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Cronos;

using EasyCronJob.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geex.Common.BackgroundJob
{
    public abstract class CronJob<TImplementation> : CronJobService where TImplementation : class, IHostedService
    {
        public abstract bool IsConcurrentAllowed { get; }
        public bool IsBusy { get; private set; }
        public static void Register(IServiceCollection services, string cron)
        {
            services.AddHostedService<TImplementation>(x => Activator.CreateInstance(typeof(TImplementation), new object[] { x, cron }).As<TImplementation>());
        }
        private readonly ILogger<TImplementation> _logger;

        /// <inheritdoc />
        public CronJob(IServiceProvider sp, string cronExp)
            : base(cronExp, TimeZoneInfo.Local, cronExp.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 6 ? CronFormat.IncludeSeconds : CronFormat.Standard)
        {
            this._logger = sp.GetService<ILogger<TImplementation>>();
            this.ServiceProvider = sp;
        }

        private IServiceProvider ServiceProvider { get; set; }

        /// <inheritdoc />
        protected override Task ScheduleJob(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job scheduled: [{JobName}]", typeof(TImplementation).Name);
            return base.ScheduleJob(cancellationToken);
        }

        /// <inheritdoc />
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job starting: [{JobName}]", typeof(TImplementation).Name);
            try
            {
                await base.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogErrorWithData(e, "Job failed to start: [{JobName}]", typeof(TImplementation).Name);
            }
            _logger.LogInformation("Job started: [{JobName}]", typeof(TImplementation).Name);
        }

        /// <summary>
        /// 真正执行逻辑
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task DoWork(CancellationToken cancellationToken)
        {
            if (IsBusy && !IsConcurrentAllowed)
            {
                return;
            }
            IsBusy = true;
            _logger.LogInformation("Job processing: [{JobName}]", typeof(TImplementation).Name);
            using var scope = this.ServiceProvider.CreateScope();
            try
            {
                using var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await this.Run(scope.ServiceProvider, cancellationToken);
                await uow.CommitAsync();
                _logger.LogInformation("Job processed: [{JobName}]", typeof(TImplementation).Name);
            }
            catch (Exception e)
            {
                _logger.LogErrorWithData(e, "Job failed: [{JobName}]", typeof(TImplementation).Name);
                await this.OnException(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected virtual Task OnException(Exception exception)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job stopping: [{JobName}]", typeof(TImplementation).Name);
            try
            {
                await base.StopAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogErrorWithData(e, "Job failed to stop: [{JobName}]", typeof(TImplementation).Name);
            }
            _logger.LogInformation("Job stopped: [{JobName}]", typeof(TImplementation).Name);

        }
    }
}
