using HotChocolate;

using Microsoft.Extensions.Logging;

namespace Geex.Abstractions
{
    public class LoggingErrorFilter : IErrorFilter
    {
        public LoggingErrorFilter(ILoggerFactory? loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public ILoggerFactory LoggerFactory { get; }

        public IError OnError(IError error)
        {
            if (error.Exception != default)
            {
                if (error.Exception?.TargetSite?.DeclaringType != default)
                {
                    var logger = LoggerFactory.CreateLogger(error.Exception.TargetSite.DeclaringType.FullName);
                    logger.LogException(error.Exception);
                }
                else
                {
                    var logger = LoggerFactory.CreateLogger("Null");
                    logger.LogException(error.Exception);
                }
            }
            return error;
        }
    }
}
