using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstraction.Tasking
{
    public class PowerShellScriptRunner
    {
        static TimeSpan defaultTimeout = TimeSpan.FromSeconds(10);

        [Logging]
        public static async Task<string?> ExecutePowerShell(string workDirectory, string command, string? outputIndicator = default, TimeSpan? timeout = default)
        {
            // 创建一个PowerShell进程
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDirectory
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.Start();

            // 等待PowerShell进程完成
            try
            {
                await process.WaitForExitAsync(new CancellationTokenSource(timeout ?? defaultTimeout).Token);
            }
            catch (TaskCanceledException e)
            {
                var error = string.Empty;
                try
                {
                    error = await process.StandardError.ReadToEndAsync();
                }
                catch (Exception)
                {
                    // ignored
                }

                Logger.LogError(e, $"{nameof(ExecutePowerShell)} timed out: {error}");
                return default;
            }

            // 读取PowerShell进程的输出
            if (process.ExitCode < 0)
            {
                // 处理错误信息
                string errors = await process.StandardError.ReadToEndAsync();
                throw new Exception(errors);
            }
            else
            {
                // 处理输出信息
                var processOutput = process.StandardOutput;

                if (outputIndicator.IsNullOrEmpty()) return await processOutput.ReadToEndAsync();

                while (await processOutput.ReadLineAsync() is { } block)
                {
                    if (block == outputIndicator)
                    {
                        return await processOutput.ReadToEndAsync();
                    }
                }
                return await processOutput.ReadToEndAsync();
            }
        }

        public static ILogger<PowerShellScriptRunner> Logger => ServiceLocator.Global.GetService<ILogger<PowerShellScriptRunner>>();
    }
}
