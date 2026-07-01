using System;

using Geex.Gql;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class ObjectFieldExtensions
{
    extension(IOutputField outputField)
    {
        public GeexFeatures GeexFeatures => new(outputField.ContextData);
    }

    public static bool IsSystemOrIntrospectionField(this IObjectField field) =>
        field.Name is "_" ||
        field.IsIntrospectionField ||
        field.Name.StartsWith("__", StringComparison.Ordinal);
}
