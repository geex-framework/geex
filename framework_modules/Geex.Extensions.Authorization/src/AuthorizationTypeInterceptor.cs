using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Fasterflect;

using Geex.Abstractions;
using Geex.Common;
using Geex.Gql.Types;

using HotChocolate.Authorization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Geex.Gql
{
    /// <summary>
    /// Type interceptor for handling authorization on fields
    /// </summary>
    public class AuthorizationTypeInterceptor : TypeInterceptor
    {
        static Type AuthorizeMiddlewareType = typeof(AuthorizeDirective).Assembly.GetType("HotChocolate.Types.AuthorizeMiddleware", true);
        /// <summary>
        /// mod_query_user, mod_query_user_email
        /// </summary>
        Dictionary<string, string[]> Permissions = AppPermission.List
            .GroupBy(x => x.Obj).ToDictionary(x => x.Key, x => x.Select(y => y.Field).ToArray());

        ///// <inheritdoc />
        //public override void OnBeforeCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
        //{
        //    if (definition is ObjectTypeDefinition objectTypeDefinition)
        //    {
        //        var runtimeTypeName = objectTypeDefinition.RuntimeType.Name;
        //        ApplyImplicitAuthorization(objectTypeDefinition, completionContext, runtimeTypeName.ToCamelCase());
        //    }
        //    base.OnBeforeCompleteName(completionContext, definition);
        //}



        /// <inheritdoc />
        public override void OnAfterCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var runtimeTypeName = objectTypeDefinition.RuntimeType.Name;
                ApplyImplicitAuthorization(objectTypeDefinition, completionContext, runtimeTypeName.ToCamelCase());
            }
            base.OnAfterCompleteName(completionContext, definition);
        }

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var runtimeTypeName = objectTypeDefinition.RuntimeType.Name;
                ApplyImplicitAuthorization(objectTypeDefinition, completionContext, runtimeTypeName.ToCamelCase());
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private void ApplyImplicitAuthorization(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext, string typeName)
        {
            var fields = objectTypeDefinition.Fields;

            var logger = completionContext.Services.GetService<ILogger<AuthorizationTypeInterceptor>>();
            if (Permissions.TryGetValue(typeName, out string[] fieldPermissions))
            {
                var misMatches = fieldPermissions.Except(fields.Select(x => x.Name)).ToList();
                var matches = fields.Join(fieldPermissions, l => l.Name, r => r, (l, r) => l);
                foreach (var fieldDefinition in matches)
                {
                    var policy = $"{typeName}_{fieldDefinition.Name}";
                    if (!fieldDefinition.Directives.Any(x => (x.Value is AuthorizeDirective)))
                    {
                        fieldDefinition.AddDirective(new AuthorizeDirective(policy), completionContext.TypeInspector);
                    }
                    logger.LogInformation($@"成功匹配权限规则:{policy}");
                }
                foreach (var misMatch in misMatches)
                {
                    var policy = $"{typeName}_{misMatch}";
                    logger.LogWarning($@"跳过匹配权限规则:{policy}");
                }
            }
        }

        ///// <summary>
        ///// Apply authorization to extension types (Query/Mutation/Subscription)
        ///// </summary>
        //public void ApplyExtensionTypeAuthorization(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext)
        //{
        //    var runtimeType = objectTypeDefinition.RuntimeType;
        //    var fields = objectTypeDefinition.Fields;
        //    var logger = completionContext.Services.GetService<ILogger<AuthorizationTypeInterceptor>>();

        //    // Only process extension types
        //    if (!typeof(ObjectTypeExtension).IsAssignableFrom(runtimeType))
        //    {
        //        return;
        //    }

        //    // Get caller information to determine prefix
        //    var trace = new StackTrace();
        //    var caller = trace.GetFrame(1).GetMethod();
        //    var callerDeclaringType = caller.DeclaringType;

        //    var prefixMatchModules = GeexModule.ModuleTypes.Where(x =>
        //        callerDeclaringType.Namespace.Contains(
        //            x.Namespace.RemovePostFix("Gql", "Api", "Core", "Tests"),
        //            StringComparison.InvariantCultureIgnoreCase));

        //    var module = prefixMatchModules.OrderByDescending(x => x.Name.Length).FirstOrDefault();
        //    var moduleName = module.Namespace.Split(".").ToList()
        //        .Last(x => !x.IsIn("Gql", "Api", "Core", "Tests"))
        //        .ToCamelCase();

        //    var className = callerDeclaringType.Name;
        //    var operationTypePrefix = "";

        //    if (className.Contains("Query"))
        //    {
        //        operationTypePrefix = $"{moduleName}_query";
        //    }
        //    else if (className.Contains("Mutation"))
        //    {
        //        operationTypePrefix = $"{moduleName}_mutation";
        //    }
        //    else if (className.Contains("Subscription"))
        //    {
        //        operationTypePrefix = $"{moduleName}_subscription";
        //    }

        //    foreach (var field in fields)
        //    {
        //        if (field.Member is MemberInfo memberInfo)
        //        {
        //            var policy = $"{operationTypePrefix}_{memberInfo.Name.RemovePreFix("Get").ToCamelCase()}";

        //            // Check if there's a matching permission
        //            if (AppPermission.List.Any(x => x.Value == policy))
        //            {
        //                field.Directives.Add(new DirectiveDefinition(new DirectiveNode(
        //                    "authorize",
        //                    new ArgumentNode("policy", policy))));

        //                logger?.LogInformation($@"成功匹配权限规则:{policy} for {memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
        //            }
        //            else
        //            {
        //                logger?.LogWarning($@"跳过匹配权限规则:{memberInfo.DeclaringType?.Name}.{memberInfo.Name}");
        //            }
        //        }
        //    }
        //}
    }
}
