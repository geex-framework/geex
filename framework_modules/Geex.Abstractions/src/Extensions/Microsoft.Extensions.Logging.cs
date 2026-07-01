using System;
using System.Text;

using Geex;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Logging
{
  public static class LoggerExtensions
  {
    private static void _LogWithDataInternal(ILogger logger, LogLevel level, EventId? eventId, Exception exception, string message)
    {
      if (eventId.HasValue)
      {
        logger.Log(level, eventId.Value, exception, message);
      }
      else
      {
        logger.Log(level, exception, message);
      }
    }
    // 通用的日志方法
    extension(ILogger logger)
    {
      private void LogWithData(LogLevel level,
        string message,
        EventId? eventId = null,
        Exception exception = null,
        object data = null
      )
      {
        var sb = new StringBuilder();
        sb.AppendLine(message);
        sb.AppendLine($"---------- data -----------");
        sb.AppendLine(data?.ToJsonSafe(x =>
        {
          x.WriteIndented = true;
        }));

        _LogWithDataInternal(logger, level, eventId, exception, sb.ToString());
      }

      public void LogTraceWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Trace, eventId, exception, message, data);

      public void LogTraceWithData(EventId eventId, string message, object data = null) =>
        logger.LogTraceWithData(eventId, message, null, data);

      public void LogTraceWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Trace, exception, message, data);

      public void LogTraceWithData(string message, object data = null) =>
        logger.LogTraceWithData(message, null, data);

      public void LogDebugWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Debug, eventId, exception, message, data);

      public void LogDebugWithData(EventId eventId, string message, object data = null) =>
        logger.LogDebugWithData(eventId, message, null, data);

      public void LogDebugWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Debug, exception, message, data);

      public void LogDebugWithData(string message, object data = null) =>
        logger.LogDebugWithData(message, null, data);

      public void LogInformationWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Information, eventId, exception, message, data);

      public void LogInformationWithData(EventId eventId, string message, object data = null) =>
        logger.LogInformationWithData(eventId, message, null, data);

      public void LogInformationWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Information, exception, message, data);

      public void LogInformationWithData(string message, object data = null) =>
        logger.LogInformationWithData(message, null, data);

      public void LogWarningWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Warning, eventId, exception, message, data);

      public void LogWarningWithData(EventId eventId, string message, object data = null) =>
        logger.LogWarningWithData(eventId, message, null, data);

      public void LogWarningWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Warning, exception, message, data);

      public void LogWarningWithData(string message, object data = null) =>
        logger.LogWarningWithData(message, null, data);

      public void LogErrorWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Error, eventId, exception, message, data);

      public void LogErrorWithData(EventId eventId, string message, object data = null) =>
        logger.LogErrorWithData(eventId, message, null, data);

      public void LogErrorWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Error, exception, message, data);

      public void LogErrorWithData(string message, object data = null) =>
        logger.LogErrorWithData(message, null, data);

      public void LogCriticalWithData(EventId eventId, string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Critical, eventId, exception, message, data);

      public void LogCriticalWithData(EventId eventId, string message, object data = null) =>
        logger.LogCriticalWithData(eventId, message, null, data);

      public void LogCriticalWithData(string message, Exception exception = null, object data = null) =>
        Log(logger, LogLevel.Critical, exception, message, data);

      public void LogCriticalWithData(string message, object data = null) =>
        logger.LogCriticalWithData(message, null, data);
    }

    private static void Log(ILogger logger, LogLevel level, EventId eventId, Exception exception, string message, object data = null)
    {
      LogWithData(logger, level, message, eventId, exception, data);
    }

    private static void Log(ILogger logger, LogLevel level, Exception exception, string message, object data = null)
    {
      LogWithData(logger, level, message, default, exception, data);
    }
  }
}
