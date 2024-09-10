using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstraction.Tasking
{
    public class PwshScriptRunner
    {
        static TimeSpan defaultTimeout = TimeSpan.FromSeconds(10);

        [Logging]
        public static async Task<string?> Execute(string workDirectory, string command, string? outputIndicator = default, TimeSpan? timeout = default)
        {
            // 创建一个pwsh进程
            var processInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments =
                    $"-ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDirectory,
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.EnableRaisingEvents = true;
            process.Start();
            var resultMessage = default(string);
            // 等待PowerShell进程完成
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(timeout ?? defaultTimeout);
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (process.HasExited) break;
                        var processTime = process.TotalProcessorTime;
                        await Task.Delay(1000);
                        if (process.HasExited) break;
                        if (processTime == process.TotalProcessorTime)
                        {
                            try
                            {
                                resultMessage = "<no output>";
                                process.Close();
                            }
                            catch (Exception e)
                            {
                                process.Kill();
                                Logger.LogError("PwshScriptRunner process killed for command '{command}'", command);
                            }
                            finally
                            {
                                await cancellationTokenSource.CancelAsync();
                            }
                        }
                    }
                });
                await process.WaitForExitAsync(cancellationTokenSource.Token);
            }
            catch (TaskCanceledException e)
            {
                var error = string.Empty;
                try
                {
                    if (resultMessage == "<no output>")
                    {
                        return resultMessage;
                    }

                    error = await process.StandardError.ReadToEndAsync();
                    Logger.LogError(e, "PwshScriptRunner process cancelled for command '{command}' with message '{message}'", command, error);
                }
                catch (Exception)
                {
                    // ignored
                }

                return default;
            }

            // 读取PowerShell进程的输出
            if (process.ExitCode < 0)
            {
                // 处理错误信息
                string errors = await process.StandardError.ReadToEndAsync();
                throw new Exception(errors);
            }

            // 处理输出信息
            var processOutput = process.StandardOutput;

            if (outputIndicator.IsNullOrEmpty())
            {
                resultMessage = await processOutput.ReadToEndAsync();
                return resultMessage;
            }
            else
            {
                while (await processOutput.ReadLineAsync() is { } block)
                {
                    if (block == outputIndicator)
                    {
                        resultMessage = await processOutput.ReadToEndAsync();
                        return resultMessage;
                    }
                }
                return resultMessage;
            }
        }

        public static ILogger<PwshScriptRunner> Logger => ServiceLocator.Global.GetService<ILogger<PwshScriptRunner>>();
    }
}
