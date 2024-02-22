﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp.DependencyInjection;

namespace Geex.Common.BackgroundJob
{
    public class FireAndForgetTaskScheduler
    {
        private readonly IServiceProvider _rootServiceProvider;

        public FireAndForgetTaskScheduler(IServiceProvider rootServiceProvider)
        {
            _rootServiceProvider = rootServiceProvider;
        }

        public void Schedule<TTask, TParam>(FireAndForgetTask<TTask, TParam> task, int? delay = default)
        {
            // Fire off the task, but don't await the result
            Task.Run(async () =>
            {
                // Exceptions must be caught
                try
                {
                    if (delay.HasValue)
                    {
                        await Task.Delay(delay.Value);
                    }
                    using var scope = _rootServiceProvider.CreateScope();
                    task.ServiceProvider = scope.ServiceProvider;
                    await task.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
    }

}