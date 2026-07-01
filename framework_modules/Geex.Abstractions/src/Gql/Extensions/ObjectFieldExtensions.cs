using System;

using HotChocolate.Types;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class ObjectFieldExtensions
{
    public static bool IsSystemOrIntrospectionField(this IObjectField field) =>
        field.Name is "_" ||
        field.IsIntrospectionField ||
        field.Name.StartsWith("__", StringComparison.Ordinal);
}
