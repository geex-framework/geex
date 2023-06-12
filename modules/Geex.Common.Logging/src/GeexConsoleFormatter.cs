using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Geex.Common.Logging
{
    public class GeexConsoleFormatter : ConsoleFormatter
    {
        public GeexConsoleFormatter() : base(nameof(GeexConsoleFormatter))
        {
        }


        public GeexConsoleFormatter(ConsoleFormatterOptions _)
            : base(nameof(GeexConsoleFormatter))
        {
        }

        public GeexConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> _)
            : base(nameof(GeexConsoleFormatter))
        {
        }

        const string DefaultForegroundColor = "\x1B[39m\x1B[22m";
        const string DefaultBackgroundColor = "\x1B[49m";
        static string GetForegroundColorEscapeCode(ConsoleColor color) =>
        color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",

            _ => DefaultForegroundColor
        };

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter writer)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
            {
                return;
            }

            // Example:
            // 2018-07-30T22:29:32.001 [info] [Program] Request received

            string logLevelString = GetLogLevelString(logEntry.LogLevel);
            string className = logEntry.Category.Substring(logEntry.Category.LastIndexOf('.') + 1);

            // Add log level, UTC DateTime in ISO 8601 format and class name
            writer.Write($"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)} [{logLevelString}] [{className}] ");

            // Add the message
            if (!string.IsNullOrEmpty(message))
            {
                writer.WriteLine(message);
            }

            if (logEntry.State is object[] state)
            {
                if (state.Length == 1)
                {
                    writer.WriteLine(state[0].ToJsonSafe());
                }
                else
                {
                    foreach (var o in state)
                    {
                        writer.WriteLine(o.ToJsonSafe());
                    }
                }
            }

            // Add the exception with stack trace
            if (logEntry.Exception != null)
            {
                writer.Write(Environment.NewLine);
                writer.WriteLine(logEntry.Exception.ToString());
            }
        }

        /// <summary>
        /// Maps a <see cref="LogLevel"/> to a string
        /// </summary>
        /// <param name="logLevel">The log level</param>
        /// <returns>String representation of the log level</returns>
        private string GetLogLevelString(LogLevel logLevel)
        {
            var color = GetForegroundColorEscapeCode(ConsoleColor.White);
            switch (logLevel)
            {
                case LogLevel.Trace:
                    color = GetForegroundColorEscapeCode(ConsoleColor.DarkGray); break;
                case LogLevel.Debug:
                    color = GetForegroundColorEscapeCode(ConsoleColor.White); break;
                case LogLevel.Information:
                    color = GetForegroundColorEscapeCode(ConsoleColor.Green); break;
                case LogLevel.Warning:
                    color = GetForegroundColorEscapeCode(ConsoleColor.Yellow); break;
                case LogLevel.Error:
                    color = GetForegroundColorEscapeCode(ConsoleColor.Red); break;
                case LogLevel.Critical:
                    color = GetForegroundColorEscapeCode(ConsoleColor.DarkRed); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
            return color + logLevel + DefaultForegroundColor;
        }
    }
}
