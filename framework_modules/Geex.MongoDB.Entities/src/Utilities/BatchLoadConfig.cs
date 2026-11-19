using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Entities.Utilities
{
    public readonly struct BatchLoadPathKey : IEquatable<BatchLoadPathKey>
    {
        public Type DeclaringEntityType { get; }
        public string PropertyName { get; }
        internal IReadOnlyList<Type> ExcludedEntityTypes { get; }

        public BatchLoadPathKey(Type declaringEntityType, string propertyName)
            : this(declaringEntityType, propertyName, Array.Empty<Type>()) { }

        internal BatchLoadPathKey(Type declaringEntityType, string propertyName, IEnumerable<Type> excludedEntityTypes)
        {
            DeclaringEntityType = declaringEntityType;
            PropertyName = propertyName;
            ExcludedEntityTypes = excludedEntityTypes.Distinct().OrderBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal).ToArray();
        }

        public bool Equals(BatchLoadPathKey other) =>
            PropertyName == other.PropertyName &&
            DeclaringEntityType == other.DeclaringEntityType &&
            (ExcludedEntityTypes ?? Array.Empty<Type>()).SequenceEqual(other.ExcludedEntityTypes ?? Array.Empty<Type>());

        public override bool Equals(object? obj) => obj is BatchLoadPathKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(DeclaringEntityType);
            hash.Add(PropertyName);
            foreach (var type in ExcludedEntityTypes ?? Array.Empty<Type>()) hash.Add(type);
            return hash.ToHashCode();
        }
    }

    internal sealed class BatchLoadPathNode
    {
        public BatchLoadPathNode(PropertyInfo property, BatchLoadPathKey key)
        {
            Property = property;
            Key = key;
        }

        public PropertyInfo Property { get; }
        public BatchLoadPathKey Key { get; }
        public Type DeclaringEntityType => Key.DeclaringEntityType;
        public BatchLoadConfig Children { get; } = new();
        public bool AppliesTo(IEntityBase entity) =>
            DeclaringEntityType.IsInstanceOfType(entity) &&
            !Key.ExcludedEntityTypes.Any(type => type.IsInstanceOfType(entity));
    }

    public class BatchLoadConfig
    {
        internal Dictionary<BatchLoadPathKey, BatchLoadPathNode> SubBatchLoadConfigs { get; } = new();
    }
}
