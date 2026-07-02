using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.Attributes;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class AutoBatchLoadDependsOnExtensions
    {
        public static IReadOnlyList<string> GetAutoBatchLoadDependsOnNavigationNames(this PropertyInfo property) =>
            property.GetCustomAttributes<AutoBatchLoadDependsOnAttribute>()
                .Select(attribute => attribute.NavigationPropertyName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToArray();
    }
}
