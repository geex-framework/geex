using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Tasking
{
    public class PwshScriptRunner
    {
        static TimeSpan defaultTimeout = TimeSpan.FromSeconds(600);

        [Logging]
        public static async Task<string?> Execute(string workDirectory, string command, string? outputMatchRegex = default, TimeSpan? timeout = default)
        {
            var outputStr = await ExecuteInternal(workDirectory, command, timeout);
            if (outputStr == null) return null;

            if (outputMatchRegex.IsNullOrEmpty())
            {
                return outputStr;
            }
            else
            {
                var regex = new Regex(outputMatchRegex, RegexOptions.Compiled);
                var match = regex.Match(outputStr);
                if (match.Success)
                {
                    return match.Groups[0].Value;
                }
                return null;
            }
        }

        [Logging]
        public static async Task<Match?> Execute(string workDirectory, string command, Regex outputMatchRegex, TimeSpan? timeout = default)
        {
            var outputStr = await ExecuteInternal(workDirectory, command, timeout);
            if (outputStr == null) return null;

            var match = outputMatchRegex.Match(outputStr);
            return match.Success ? match : null;
        }

        private static async Task<string?> ExecuteInternal(string workDirectory, string command, TimeSpan? timeout)
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
            var output = new StringBuilder();
            var error = new StringBuilder();

            var outputComplete = new TaskCompletionSource<bool>();
            var errorComplete = new TaskCompletionSource<bool>();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    outputComplete.SetResult(true);
                }
                else
                {
                    output.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    errorComplete.SetResult(true);
                }
                else
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutValue = timeout.GetValueOrDefault(defaultTimeout);
            using var cts = new CancellationTokenSource(timeoutValue);
            
            try
            {
                // 等待进程完成或超时
                var processTask = Task.Run(() => process.WaitForExit());
                var completedTask = await Task.WhenAny(processTask, Task.Delay(timeoutValue, cts.Token));
                
                if (completedTask == processTask)
                {
                    // 进程正常完成，等待输出流完成
                    await Task.WhenAll(outputComplete.Task, errorComplete.Task).ConfigureAwait(false);
                    
                    if (process.ExitCode != 0)
                    {
                        // 处理错误信息
                        throw new Exception(error.ToString());
                    }
                    else
                    {
                        return output.ToString();
                    }
                }
                else
                {
                    // 超时处理
                    Logger.LogError("PwshScriptRunner process timeout for command '{command}'", command);
                    
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill(true); // 终止进程及其子进程
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("Failed to kill process: {exception}", ex.Message);
                        }
                    }
                    
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogError("PwshScriptRunner process cancelled for command '{command}'", command);
                
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Failed to kill process: {exception}", ex.Message);
                    }
                }
                
                return null;
            }
        }

        public static ILogger<PwshScriptRunner> Logger => ServiceLocator.Global.GetService<ILogger<PwshScriptRunner>>();
    }
}
