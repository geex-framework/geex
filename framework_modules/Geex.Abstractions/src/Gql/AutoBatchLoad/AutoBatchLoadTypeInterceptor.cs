using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex;
using Geex.Gql;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Gql.AutoBatchLoad
{
    /// <summary>
    /// Type interceptor for injecting AutoBatchLoad middleware on operation fields
    /// </summary>
    public class AutoBatchLoadTypeInterceptor : TypeInterceptor
    {
        // Single middleware instance reused for all fields
        private static readonly AutoBatchLoadMiddleware _autoBatchLoadMiddleware = new();

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                ApplyAutoBatchLoad(objectTypeDefinition, completionContext);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private void ApplyAutoBatchLoad(
            ObjectTypeDefinition objectTypeDefinition,
            ITypeCompletionContext completionContext)
        {
            if (!objectTypeDefinition.IsOperationExtensionType())
            {
                return;
            }

            var globalAutoBatchLoad = completionContext.Services
                .GetRequiredService<GeexCoreModuleOptions>()
                .AutoBatchLoad;

            foreach (var fieldDefinition in objectTypeDefinition.Fields)
            {
                if (IsSystemOrIntrospectionField(fieldDefinition))
                {
                    continue;
                }

                if (!IsEntityReturningField(fieldDefinition))
                {
                    continue;
                }

                UseAutoBatchLoadExtensions.EnsureAutoBatchLoadConfigured(fieldDefinition, globalAutoBatchLoad);

                if (fieldDefinition.GeexFeatures.AutoBatchLoad?.IsEnabled != true)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(completionContext.DescriptorContext, fieldDefinition);

                descriptor.Use(next => async context =>
                    await _autoBatchLoadMiddleware.InvokeAsync(context, next));
            }
        }

        private static bool IsSystemOrIntrospectionField(ObjectFieldDefinition field) =>
            field.Name is "_" ||
            field.IsIntrospectionField ||
            field.Name.StartsWith("__", StringComparison.Ordinal);

        private static bool IsEntityReturningField(
            ObjectFieldDefinition field,
            EntityReturningKind kinds = EntityReturningKind.All) =>
            TryGetEntityReturningKind(field, out var kind, out _) && (kind & kinds) != 0;

        private static bool TryGetEntityReturningKind(
            ObjectFieldDefinition field,
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
}
