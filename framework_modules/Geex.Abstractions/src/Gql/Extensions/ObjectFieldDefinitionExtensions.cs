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
    public static bool IsEntityReturningField(
        this ObjectFieldDefinition field,
        EntityReturningKind kinds = EntityReturningKind.All) =>
        field.TryGetEntityReturningKind(out var kind, out _) && (kind & kinds) != 0;

    public static bool TryGetEntityReturningKind(
        this ObjectFieldDefinition field,
        out EntityReturningKind kind,
        out Type entityType)
    {
        kind = EntityReturningKind.None;
        entityType = null!;

        foreach (var returnType in GetDeclaredReturnTypes(field))
        {
            if (returnType.TryGetEntityReturningKind(out kind, out entityType))
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
