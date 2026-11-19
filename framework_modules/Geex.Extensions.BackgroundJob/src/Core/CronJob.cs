using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Cronos;

using EasyCronJob.Abstractions;
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
            : this(sp, cronExp, TimeZoneInfo.Local)
        {
        }

        protected CronJob(IServiceProvider sp, string cronExp, TimeZoneInfo timeZone)
            : base(cronExp, timeZone, cronExp.Trim().Count(x => x == ' ') == 5 ? CronFormat.IncludeSeconds : CronFormat.Standard)
        {
            ScheduleTimeZone = timeZone;
            this._logger = sp.GetService<ILogger<CronJobService>>();
            this.Cron = CronExpression.Parse(cronExp, cronExp.Trim().Count(x => x == ' ') == 5 ? CronFormat.IncludeSeconds : CronFormat.Standard);
            this.ServiceProvider = sp;
        }

        public CronExpression Cron { get; private set; }

        public TimeZoneInfo ScheduleTimeZone { get; }

        private IServiceProvider ServiceProvider { get; set; }

        private readonly object _timerGate = new();
        private System.Timers.Timer? _timer;

        /// <inheritdoc />
        protected override Task ScheduleJob(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }
            var now = DateTimeOffset.UtcNow;
            var next = this.Cron.GetNextOccurrence(now, ScheduleTimeZone);
            if (next is null)
            {
                return Task.CompletedTask;
            }
            // EasyCronJob 2.1.0 在边界秒会以非正间隔创建 System.Timers.Timer 并抛
            // ArgumentException 杀死进程；这里自行调度并保证间隔至少 1ms。
            var delay = next.Value - now;
            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromMilliseconds(1);
            }
            var interval = Math.Max(1d, delay.TotalMilliseconds);
            var timer = new System.Timers.Timer(interval) { AutoReset = false };
            timer.Elapsed += async (_, _) =>
            {
                timer.Dispose();
                lock (_timerGate)
                {
                    if (ReferenceEquals(_timer, timer)) _timer = null;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                await DoWork(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken);
                }
            };
            lock (_timerGate)
            {
                _timer?.Dispose();
                _timer = timer;
            }
            timer.Start();
            _logger.LogInformation("Job scheduled: [{JobName}], next execution will be at {next}", typeof(TImplementation).Name, next.Value.ToLocalTime());
            return Task.CompletedTask;
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
                lock (_timerGate)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
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
