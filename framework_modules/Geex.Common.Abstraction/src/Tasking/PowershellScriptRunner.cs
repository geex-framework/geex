﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstraction.Tasking
{
    public class PowerShellScriptRunner
    {
        public static async Task<string?> ExecutePowerShell(string workDirectory, string command, string? outputIndicator)
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
                await process.WaitForExitAsync(new CancellationTokenSource(10000).Token);
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
