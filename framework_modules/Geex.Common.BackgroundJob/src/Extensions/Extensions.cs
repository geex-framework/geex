using System;
using System.Collections.Generic;

using EasyCronJob.Abstractions;

using Geex.Abstractions.Authentication;
using Geex.Common.BackgroundJob;
using Geex.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class Microsoft_Extensions_DependencyInjection
    {
        /// <summary>
        /// 注册job, 如果不传入cron表达式则默认读取配置[BackgroundJobModuleOptions:JobConfigs:<Job名称>]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="cronExp"></param>
        /// <exception cref="Exception"></exception>
        public static void AddJob<T>(this IServiceCollection services, string? cronExp = default) where T : CronJob<T>
        {
            var jobType = typeof(T);
            var jobName = jobType.Name;
            var backgroundJobModuleOptions = services.GetSingletonInstance<BackgroundJobModuleOptions>();
            if (backgroundJobModuleOptions.Disabled)
            {
                return;
            }
            cronExp ??= backgroundJobModuleOptions.JobConfigs.GetValueOrDefault(jobName);
            if (cronExp.IsNullOrEmpty())
            {
                throw new Exception($"[{jobName}]未配置, 查找配置结点: [{nameof(BackgroundJobModuleOptions)}:{nameof(BackgroundJobModuleOptions.JobConfigs)}:{jobName}]");
            }

            var register = typeof(CronJob<>).MakeGenericType(jobType).GetMethod(nameof(CronJob<CronJobService>.Register));
            register.Invoke(null, new object[] { services, cronExp });
        }

        public static void ScheduleFireAndForgetTask(this IUnitOfWork uow, IFireAndForgetTask task, bool afterSaveChange, int? delay = default)
        {
            var taskScheduler = uow.ServiceProvider.GetRequiredService<FireAndForgetTaskScheduler>();
            if (afterSaveChange)
            {
                uow.PostSaveChanges += async () =>
                {
                    taskScheduler.Schedule(task, delay);
                };
            }
            else
            {
                taskScheduler.Schedule(task, delay);
            }
        }
    }
}
