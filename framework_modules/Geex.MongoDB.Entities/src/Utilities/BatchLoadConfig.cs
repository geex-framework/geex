using System;
using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Entities.Utilities
{
    public readonly struct BatchLoadPathKey : IEquatable<BatchLoadPathKey>
    {
        public Type DeclaringEntityType { get; }
        public string PropertyName { get; }

        public BatchLoadPathKey(Type declaringEntityType, string propertyName)
        {
            DeclaringEntityType = declaringEntityType;
            PropertyName = propertyName;
        }

        public bool Equals(BatchLoadPathKey other) =>
            PropertyName == other.PropertyName &&
            DeclaringEntityType == other.DeclaringEntityType;

        public override bool Equals(object? obj) => obj is BatchLoadPathKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(DeclaringEntityType, PropertyName);
    }

    internal sealed class BatchLoadPathNode
    {
        public BatchLoadPathNode(PropertyInfo property, Type declaringEntityType)
        {
            Property = property;
            DeclaringEntityType = declaringEntityType;
        }

        public PropertyInfo Property { get; }
        public Type DeclaringEntityType { get; }
        public BatchLoadConfig Children { get; } = new();
    }

    public class BatchLoadConfig
    {
        internal Dictionary<BatchLoadPathKey, BatchLoadPathNode> SubBatchLoadConfigs { get; } = new();
    }
}
