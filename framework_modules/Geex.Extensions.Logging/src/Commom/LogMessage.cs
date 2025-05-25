using System;

namespace Geex.Extensions.Logging.Commom
{
    public struct LogMessage
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; set; }
    }
}
