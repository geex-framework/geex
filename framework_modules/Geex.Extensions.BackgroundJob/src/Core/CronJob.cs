using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cronos;

using EasyCronJob.Abstractions;

using Geex.Extensions.BackgroundJob.Gql.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.BackgroundJob.Core
{
    public abstract class CronJob<TImplementation> : CronJobService where TImplementation : class, IHostedService
    {
        public abstract bool IsConcurrentAllowed { get; }
        public bool IsBusy { get; private set; }
        public static void Register(IServiceCollection services, string cron)
        {
            services.AddHostedService<TImplementation>(x => Activator.CreateInstance(typeof(TImplementation), new object[] { x, cron }).As<TImplementation>());
        }
        private readonly ILogger<CronJobService> _logger;

        /// <summary>
        /// note: service provider here is singleton root service provider
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="cronExp"></param>
        public CronJob(IServiceProvider sp, string cronExp)
            : base(cronExp, TimeZoneInfo.Local, cronExp.Trim().Count(x => x == ' ') == 5 ? CronFormat.IncludeSeconds : CronFormat.Standard)
        {
            this._logger = sp.GetService<ILogger<CronJobService>>();
            this.Cron = CronExpression.Parse(cronExp, cronExp.Trim().Count(x => x == ' ') == 5 ? CronFormat.IncludeSeconds : CronFormat.Standard);
            this.ServiceProvider = sp;
        }

        public CronExpression Cron { get; private set; }

        private IServiceProvider ServiceProvider { get; set; }

        /// <inheritdoc />
        protected override Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = this.Cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local).GetValueOrDefault().ToLocalTime();
            _logger.LogInformation("Job scheduled: [{JobName}], next execution will be at {next}", typeof(TImplementation).Name, next);
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
                _logger.LogError(e, "Job failed to start: [{JobName}]", typeof(TImplementation).Name);
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
                _logger.LogWarning("Job is busy: [{JobName}], skipping execution", typeof(TImplementation).Name);
                return;
            }
            IsBusy = true;
            try
            {
                _logger.LogInformation("Job processing: [{JobName}]", typeof(TImplementation).Name);
                await this.Run(this.ServiceProvider, cancellationToken);
                _logger.LogInformation("Job processed: [{JobName}]", typeof(TImplementation).Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Job failed: [{JobName}]", typeof(TImplementation).Name);
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
                _logger.LogError(e, "Job failed to stop: [{JobName}]", typeof(TImplementation).Name);
            }
            _logger.LogInformation("Job stopped: [{JobName}]", typeof(TImplementation).Name);

        }
    }
}
