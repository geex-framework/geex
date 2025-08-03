using System;
using Geex.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extensions for adding the <see cref="RollingFileLoggerProvider" /> to the <see cref="ILoggingBuilder" />
    /// </summary>
    public static class RollingFileLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a file logger named 'RollingFile' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddRollingFile(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, RollingFileLoggerProvider>();
            return builder;
        }

        /// <summary>
        /// Adds a file logger named 'RollingFile' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="filename">Sets the filename prefix to use for log files</param>
        public static ILoggingBuilder AddRollingFile(this ILoggingBuilder builder, string filename)
        {
            builder.AddRollingFile(options => options.FileName = "log-");
            return builder;
        }

        /// <summary>
        /// Adds a file logger named 'RollingFile' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">Configure an instance of the <see cref="RollingFileLoggerOptions" /> to set logging options</param>
        public static ILoggingBuilder AddRollingFile(this ILoggingBuilder builder, Action<RollingFileLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            builder.AddRollingFile();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
