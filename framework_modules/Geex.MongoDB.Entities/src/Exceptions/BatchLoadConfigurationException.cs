using System;

namespace MongoDB.Entities.Exceptions
{
    public class BatchLoadConfigurationException : InvalidOperationException
    {
        public Type? EntityType { get; }
        public string? PropertyName { get; }

        public BatchLoadConfigurationException(string message) : base(message)
        {
        }

        public BatchLoadConfigurationException(Type? entityType, string? propertyName, string message)
            : base(message)
        {
            EntityType = entityType;
            PropertyName = propertyName;
        }

        public BatchLoadConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
