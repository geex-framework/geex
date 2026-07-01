using System;

using Geex.Gql.GeexFeatures;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class ObjectFieldExtensions
{
    extension(IOutputField outputField)
    {
        public GeexFeaturesAccessor GeexFeatures => new(outputField);
    }

    public static bool IsSystemOrIntrospectionField(this IObjectField field) =>
        field.Name is "_" ||
        field.IsIntrospectionField ||
        field.Name.StartsWith("__", StringComparison.Ordinal);
}
