using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal static class HotChocolate_Execution
    {

        public static string? GetOperationDetails(this IRequestContext context)
        {
            return (context.Request.OperationName?.Contains("introspection") == true ? "[Schema Query]" : context.Request.Query?.ToString());
        }
        public static string? GetOperationName(this IRequestContext context)
        {
            return (context.Operation?.Name.ToString() ?? "[unknown]");
        }

        public static string? GetOperationType(this IRequestContext context)
        {
            return context.Operation?.Type.ToString()?.ToLower() ?? "[unknown])";
        }

        public static string? GetOperationDisplay(this IRequestContext context)
        {
            return context.GetOperationType() + " " + context.GetOperationName();
        }

        public static T? GetTypedContextData<T>(this IHasContextData context, string key)
        {
            var contextData = context.ContextData;
            if (context is IMiddlewareContext middlewareContext)
            {
                if (middlewareContext.LocalContextData.TryGetValue(key, out var obj1) && obj1 is T result1)
                {
                    return result1;
                }
                if (middlewareContext.ScopedContextData.TryGetValue(key, out var obj2) && obj2 is T result2)
                {
                    return result2;
                }
            }
            if (contextData.TryGetValue(key, out var obj) && obj is T result)
            {
                return result;
            }
            return default;
        }

        private static MethodInfo EnsureCapacityMethodInfo = typeof(ObjectResult).GetMethod(nameof(EnsureCapacity), BindingFlags.NonPublic|BindingFlags.Instance);

        public static void EnsureCapacity(this ObjectResult context, int capacity)
        {
            EnsureCapacityMethodInfo.Invoke(context, new object[] { capacity });
        }

        private static MethodInfo SetValueUnsafeMethodInfo = typeof(ObjectResult).GetMethod(nameof(SetValueUnsafe), BindingFlags.NonPublic|BindingFlags.Instance, types: new[] { typeof(int), typeof(string), typeof(object), typeof(bool) });

        public static void SetValueUnsafe(this ObjectResult context, int index, string name, object? value, bool isNullable = true)
        {
            SetValueUnsafeMethodInfo.Invoke(context, new object[] { index, name, value, isNullable });
        }
    }
}
