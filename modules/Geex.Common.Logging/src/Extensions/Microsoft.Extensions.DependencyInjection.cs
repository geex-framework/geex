using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Gql;
using Geex.Common.Logging;

using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class MicrosoftExtensionsDependencyInjectionExtension
    {
        /// <summary>
        /// Adds a file logger named 'Elasticsearch' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddGeexConsole(this ILoggingBuilder builder)
        {
            builder.AddConsole(x =>
                    {
                        x.FormatterName = nameof(GeexConsoleFormatter);
                    })
                    .AddConsoleFormatter<GeexConsoleFormatter, ConsoleFormatterOptions>();
            return builder;
        }

        public static IRequestExecutorBuilder AddGeexTracing(
      this IRequestExecutorBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            builder.Services.TryAddTransient<ITimestampProvider, DefaultTimestampProvider>();
            return builder.ConfigureSchemaServices((s => s.AddSingleton<IExecutionDiagnosticEventListener, GeexTracingDiagnosticEventListener>(provider => new GeexTracingDiagnosticEventListener(provider.GetApplicationService<ILogger<GeexTracingDiagnosticEventListener>>(), provider.GetApplicationService<LoggingModuleOptions>(), provider.GetApplicationService<ITimestampProvider>()))));
        }
    }
}
