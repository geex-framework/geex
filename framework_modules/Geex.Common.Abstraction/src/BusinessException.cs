using System;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstractions
{
    public class BusinessException : Exception
    {
        public LogLevel LogLevel { get; }
        public string LogMessage { get; }
        public BusinessException(GeexExceptionType exceptionType, Exception? innerException = default,
            string? message = default) : base(message, innerException)
        {
            ExceptionName = exceptionType.Name;
            ExceptionCode = exceptionType.Value;
            LogMessage = message ?? exceptionType.DefaultLogMessage;
            LogLevel = exceptionType.LogLevel;
        }

        public BusinessException(Exception? innerException = default,
            string? message = default) : base(message, innerException)
        {
            var exceptionType = BusinessException.Default;
            ExceptionName = exceptionType.Name;
            ExceptionCode = exceptionType.Value;
            LogMessage = message ?? exceptionType.DefaultLogMessage;
            LogLevel = exceptionType.LogLevel;
        }

        public BusinessException(string? message = default) : base(message)
        {
            var exceptionType = BusinessException.Default;
            ExceptionName = exceptionType.Name;
            ExceptionCode = exceptionType.Value;
            LogMessage = message ?? exceptionType.DefaultLogMessage;
            LogLevel = exceptionType.LogLevel;
        }

        public string ExceptionCode { get; set; }

        public string ExceptionName { get; set; }
        public static GeexExceptionType Default { get; } = GeexExceptionType.Unknown;
    }

    /// <summary>
    /// inherit this enumeration to customise your own business exceptions
    /// </summary>
    public class GeexExceptionType : Enumeration<GeexExceptionType>
    {
        protected GeexExceptionType(string name, string code, string defaultLogMessage, LogLevel logLevel = LogLevel.Warning) : base(name, code)
        {
            DefaultLogMessage = defaultLogMessage;
            LogLevel = logLevel;
        }

        public string DefaultLogMessage { get; }
        public LogLevel LogLevel { get; }
        /// <summary>
        /// Data conflict
        /// </summary>
        public static GeexExceptionType Conflict { get; } = new(nameof(Conflict), nameof(Conflict), nameof(Conflict), LogLevel.Warning);
        /// <summary>
        /// on purpose, no need to handle
        /// </summary>
        public static GeexExceptionType OnPurpose { get; } = new(nameof(OnPurpose), nameof(OnPurpose), nameof(OnPurpose), LogLevel.Information);
        /// <summary>
        /// data not found
        /// </summary>
        public static GeexExceptionType NotFound { get; } = new(nameof(NotFound), nameof(NotFound), nameof(NotFound), LogLevel.Warning);
        /// <summary>
        /// external service error
        /// </summary>
        public static GeexExceptionType ExternalError { get; } = new(nameof(ExternalError), nameof(ExternalError), nameof(ExternalError), LogLevel.Warning);
        /// <summary>
        /// Unknown exception
        /// </summary>
        public static GeexExceptionType Unknown { get; } = new(nameof(Unknown), nameof(Unknown), nameof(Unknown), LogLevel.Error);
        /// <summary>
        /// requested data is not valid for operation
        /// </summary>
        public static GeexExceptionType ValidationFailed { get; } = new(nameof(ValidationFailed), nameof(ValidationFailed), nameof(ValidationFailed), LogLevel.Warning);
    }
}
