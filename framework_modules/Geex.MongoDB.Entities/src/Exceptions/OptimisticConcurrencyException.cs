using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Entities.Core.Comparers;

namespace MongoDB.Entities.Exceptions
{
    /// <summary>
    /// 字段冲突信息，包含Base/Our/Their三个值
    /// </summary>
    public class FieldConflictInfo
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 基础值（DbDataCache中的原始值）
        /// </summary>
        public object BaseValue { get; set; }

        /// <summary>
        /// 我们的值（MemoryDataCache中的当前值）
        /// </summary>
        public object OurValue { get; set; }

        /// <summary>
        /// 他们的值（数据库中的最新值）
        /// </summary>
        public object TheirValue { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public Type FieldType { get; set; }

        public override string ToString()
        {
            return $"Field: {FieldName} ({FieldType.Name}), Base: {BaseValue}, Our: {OurValue}, Their: {TheirValue}";
        }
    }

    /// <summary>
    /// 乐观锁冲突异常，当检测到并发修改冲突时抛出
    /// </summary>
    public class OptimisticConcurrencyException : Exception
    {
        /// <summary>
        /// 冲突的实体类型
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// 实体ID
        /// </summary>
        public string EntityId { get; }

        /// <summary>
        /// 冲突字段的详细信息
        /// </summary>
        public IReadOnlyList<FieldConflictInfo> ConflictingFields { get; }

        /// <summary>
        /// 我们的ModifiedOn时间
        /// </summary>
        public DateTimeOffset? BaseModifiedOn { get; }

        /// <summary>
        /// 他们的ModifiedOn时间
        /// </summary>
        public DateTimeOffset? TheirModifiedOn { get; }

        public OptimisticConcurrencyException(
            Type entityType,
            string entityId,
            IReadOnlyList<FieldConflictInfo> conflictingFields,
            DateTimeOffset? baseModifiedOn,
            DateTimeOffset? theirModifiedOn)
            : base(FormatExceptionMessage(entityType, entityId, conflictingFields, baseModifiedOn, theirModifiedOn))
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            ConflictingFields = conflictingFields ?? throw new ArgumentNullException(nameof(conflictingFields));
            BaseModifiedOn = baseModifiedOn;
            TheirModifiedOn = theirModifiedOn;
        }

        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private static string FormatExceptionMessage(
            Type entityType,
            string entityId,
            IReadOnlyList<FieldConflictInfo> conflictingFields,
            DateTimeOffset? baseModifiedOn,
            DateTimeOffset? theirModifiedOn)
        {
            var message = $"Optimistic concurrency conflict detected for entity {entityType.Name} with ID '{entityId}'.";

            if (theirModifiedOn == default)
            {
                message += " The entity was deleted by another process.";
            }
            else
            {
                message += $" Base ModifiedOn: {baseModifiedOn:yyyy-MM-dd HH:mm:ss.fff}, Their ModifiedOn: {theirModifiedOn:yyyy-MM-dd HH:mm:ss.fff}.";

                if (conflictingFields.Count > 0)
                {
                    message += $"\n\nConflicting Fields ({conflictingFields.Count}):";
                    foreach (var field in conflictingFields)
                    {
                        message += $"\n  - {field.FieldName} ({field.FieldType.Name}): Base='{field.BaseValue}', Our='{field.OurValue}', Their='{field.TheirValue}'";
                    }
                }
            }

            return message;
        }
    }
}
