using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Fasterflect;

using Geex.Abstractions;
using Geex.Common;

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Gql
{
    /// <summary>
    /// Type interceptor for handling authorization on fields
    /// </summary>
    public class AuthorizationTypeInterceptor : TypeInterceptor
    {
        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                // Apply authorization to fields
                ApplyImplicitAuthorization(objectTypeDefinition, completionContext);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private void ApplyImplicitAuthorization(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var runtimeType = objectTypeDefinition.RuntimeType;
            var fields = objectTypeDefinition.Fields;

            // Skip extension types (mutation, query, subscription) - they are handled separately
            if (typeof(ObjectTypeExtension).IsAssignableFrom(runtimeType))
            {
                return;
            }

            var logger = completionContext.Services.GetService<ILogger<AuthorizationTypeInterceptor>>();

            string GetAggregateAuthorizePrefix()
            {
                var moduleName = runtimeType.DomainName();
                var entityName = runtimeType.Name.ToCamelCase();
                var prefix = $"{moduleName}_query_{entityName}";
                return prefix;
            }

            var prefix = GetAggregateAuthorizePrefix();

            foreach (var field in fields)
            {
                if (field.Member is MemberInfo memberInfo)
                {
                    var policy = $"{prefix}_{memberInfo.Name.ToCamelCase()}";

                    // Check if there's a matching permission
                    if (AppPermission.List.Any(x => x.Value == policy))
                    {
                        field.Directives.Add(new DirectiveDefinition(
                            new DirectiveNode("authorize",
                            new ArgumentNode("policy", policy))));

                        logger?.LogInformation($@"成功匹配权限规则:{policy} for {memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
                    }
                    else
                    {
                        logger?.LogDebug($@"跳过匹配权限规则:{memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Apply authorization to extension types (Query/Mutation/Subscription)
        /// </summary>
        public void ApplyExtensionTypeAuthorization(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var runtimeType = objectTypeDefinition.RuntimeType;
            var fields = objectTypeDefinition.Fields;
            var logger = completionContext.Services.GetService<ILogger<AuthorizationTypeInterceptor>>();

            // Only process extension types
            if (!typeof(ObjectTypeExtension).IsAssignableFrom(runtimeType))
            {
                return;
            }

            // Get caller information to determine prefix
            var trace = new StackTrace();
            var caller = trace.GetFrame(1).GetMethod();
            var callerDeclaringType = caller.DeclaringType;

            var prefixMatchModules = GeexModule.ModuleTypes.Where(x =>
                callerDeclaringType.Namespace.Contains(
                    x.Namespace.RemovePostFix("Gql", "Api", "Core", "Tests"),
                    StringComparison.InvariantCultureIgnoreCase));

            var module = prefixMatchModules.OrderByDescending(x => x.Name.Length).FirstOrDefault();
            var moduleName = module.Namespace.Split(".").ToList()
                .Last(x => !x.IsIn("Gql", "Api", "Core", "Tests"))
                .ToCamelCase();

            var className = callerDeclaringType.Name;
            var operationTypePrefix = "";

            if (className.Contains("Query"))
            {
                operationTypePrefix = $"{moduleName}_query";
            }
            else if (className.Contains("Mutation"))
            {
                operationTypePrefix = $"{moduleName}_mutation";
            }
            else if (className.Contains("Subscription"))
            {
                operationTypePrefix = $"{moduleName}_subscription";
            }

            foreach (var field in fields)
            {
                if (field.Member is MemberInfo memberInfo)
                {
                    var policy = $"{operationTypePrefix}_{memberInfo.Name.RemovePreFix("Get").ToCamelCase()}";

                    // Check if there's a matching permission
                    if (AppPermission.List.Any(x => x.Value == policy))
                    {
                        field.Directives.Add(new DirectiveDefinition(new DirectiveNode(
                            "authorize",
                            new ArgumentNode("policy", policy))));

                        logger?.LogInformation($@"成功匹配权限规则:{policy} for {memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
                    }
                    else
                    {
                        logger?.LogWarning($@"跳过匹配权限规则:{memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
                    }
                }
            }
        }
    }
}
