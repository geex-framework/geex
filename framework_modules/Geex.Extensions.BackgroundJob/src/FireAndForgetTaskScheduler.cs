using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.BackgroundJob
{
    public class FireAndForgetTaskScheduler
    {
        private readonly IServiceProvider _rootServiceProvider;
        private readonly IRedisDatabase _redisDatabase;
        private ILogger<FireAndForgetTaskScheduler>? _logger;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _taskCancellationTokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        public FireAndForgetTaskScheduler(IServiceProvider rootServiceProvider)
        {
            this._rootServiceProvider = rootServiceProvider;
            this._logger = rootServiceProvider.GetService<ILogger<FireAndForgetTaskScheduler>>();
            this._redisDatabase = rootServiceProvider.GetService<IRedisDatabase>();
        }

        public async Task Schedule<TTask>(TTask task, int? delay = default) where TTask : IFireAndForgetTask
        {
            var existingTaskId = await _redisDatabase.GetAsync<string>(task.Id);
            if (string.IsNullOrEmpty(existingTaskId))
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromHours(1));
                _taskCancellationTokenSources.TryAdd(task.Id, cancellationTokenSource);
                // Fire off the task, but don't await the result
                Task.Run(async () =>
                {
                    // Exceptions must be caught
                    try
                    {
                        if (delay.HasValue)
                        {
                            await Task.Delay(delay.Value, cancellationTokenSource.Token);
                        }

                        using var scope = _rootServiceProvider.CreateScope();
                        task.ServiceProvider = scope.ServiceProvider;
                        await task.Run(cancellationTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        this._logger?.LogError(e, "Error executing fire-and-forget task {TaskId}", task.Id);
                    }
                    finally
                    {
                        _taskCancellationTokenSources.TryRemove(task.Id, out _);
                    }
                }, cancellationTokenSource.Token);
            }
            else
            {
                this._logger?.LogWarning("Task with ID {TaskId} is already scheduled and running.", task.Id);
            }
        }

        public void Cancel(string taskId)
        {
            if (_taskCancellationTokenSources.TryRemove(taskId, out var cancellationTokenSource))
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                cancellationTokenSource.Cancel();
            }
        }
    }

}
