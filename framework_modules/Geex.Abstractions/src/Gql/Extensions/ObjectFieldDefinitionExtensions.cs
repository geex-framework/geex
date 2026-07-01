using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Geex.Gql.AutoBatchLoad;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class ObjectFieldDefinitionExtensions
{
    public static bool IsQueryableEntityRootField(this ObjectFieldDefinition field) =>
        field.TryGetEntityElementType(out _);

    public static bool IsObservableEntityRootField(this ObjectFieldDefinition field) =>
        field.TryGetObservableEntityElementType(out _);

    public static bool TryGetEntityElementType(this ObjectFieldDefinition field, out Type entityType)
    {
        entityType = null!;

        if (field.ResultType.TryGetEntityElementType(out entityType))
        {
            return true;
        }

        if (field.ResolverMember is MethodInfo resolverMethod &&
            resolverMethod.ReturnType.TryGetEntityElementType(out entityType))
        {
            return true;
        }

        if (field.Member is MethodInfo memberMethod &&
            memberMethod.ReturnType.TryGetEntityElementType(out entityType))
        {
            return true;
        }

        return false;
    }

    public static bool TryGetObservableEntityElementType(this ObjectFieldDefinition field, out Type entityType)
    {
        entityType = null!;

        foreach (var returnType in GetDeclaredReturnTypes(field))
        {
            if (returnType.TryGetObservableEntityElementType(out entityType))
            {
                return true;
            }
        }

        return false;
    }

    public static void ApplyAutoBatchLoadMiddleware(this ObjectFieldDefinition definition)
    {
        if (definition.MiddlewareDefinitions.Any(x => x.Key == AutoBatchLoadMiddleware.MiddlewareKey))
        {
            return;
        }

        definition.MiddlewareDefinitions.Add(new FieldMiddlewareDefinition(
            next => async context =>
            {
                var autoBatchLoad = new AutoBatchLoadMiddleware(next);
                await autoBatchLoad.InvokeAsync(context).ConfigureAwait(false);
            },
            key: AutoBatchLoadMiddleware.MiddlewareKey));
    }

    private static IEnumerable<Type?> GetDeclaredReturnTypes(ObjectFieldDefinition field)
    {
        yield return field.ResultType;
        if (field.ResolverMember is MethodInfo resolverMethod)
        {
            yield return resolverMethod.ReturnType;
        }

        if (field.Member is MethodInfo memberMethod)
        {
            yield return memberMethod.ReturnType;
        }
    }
}
