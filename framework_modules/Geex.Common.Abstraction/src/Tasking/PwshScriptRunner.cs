using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstraction.Tasking
{
    public class PwshScriptRunner
    {
        static TimeSpan defaultTimeout = TimeSpan.FromSeconds(600);

        [Logging]
        public static async Task<string?> Execute(string workDirectory, string command, string? outputMatchRegex = default, TimeSpan? timeout = default)
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

            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
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
                    errorWaitHandle.Set();
                }
                else
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit(timeout.GetValueOrDefault(defaultTimeout)) &&
                outputWaitHandle.WaitOne(timeout.GetValueOrDefault(defaultTimeout)) &&
                errorWaitHandle.WaitOne(timeout.GetValueOrDefault(defaultTimeout)))
            {
                if (process.ExitCode < 0)
                {
                    // 处理错误信息
                    throw new Exception(error.ToString());
                }
                else
                {
                    var outputStr = output.ToString();
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
            }
            else
            {
                Logger.LogError("PwshScriptRunner process cancelled for command '{command}'", command);
                return error.ToString();
            }
        }

        public static ILogger<PwshScriptRunner> Logger => ServiceLocator.Global.GetService<ILogger<PwshScriptRunner>>();
    }
}
