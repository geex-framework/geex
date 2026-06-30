using System;
using System.Reflection;

namespace MongoDB.Entities.Utilities
{
    public class BatchLoadException : InvalidOperationException
    {
        public BatchLoadException(string message) : base(message)
        {
        }

        public static BatchLoadException NavigationNotBatchable(PropertyInfo property, Type entityType, string reason) =>
            new($"BatchLoad 导航属性 '{entityType.Name}.{property.Name}' 无效: {reason}");

        public static BatchLoadException ExecutionFailed(PropertyInfo property, Type entityType, string reason) =>
            new($"BatchLoad 执行失败 '{entityType.Name}.{property.Name}': {reason}");
    }
}
