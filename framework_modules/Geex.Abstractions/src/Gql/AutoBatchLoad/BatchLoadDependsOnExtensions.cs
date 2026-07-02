using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.Attributes;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class BatchLoadDependsOnExtensions
    {
        public static IReadOnlyList<string> GetBatchLoadDependsOnNavigationNames(this PropertyInfo property) =>
            property.GetCustomAttributes<BatchLoadDependsOnAttribute>()
                .Select(attribute => attribute.NavigationPropertyName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToArray();
    }
}
