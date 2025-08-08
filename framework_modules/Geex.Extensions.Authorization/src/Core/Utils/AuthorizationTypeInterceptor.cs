using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate.Authorization;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Authorization.Core.Utils
{
    /// <summary>
    /// Type interceptor for handling authorization on fields
    /// </summary>
    public class AuthorizationTypeInterceptor : TypeInterceptor
    {
        // Single middleware instance reused for all fields
        private static readonly AuthorizeMiddleware _authorizeMiddleware = new();

        // Cache for directives by policy
        private readonly Dictionary<string, AuthorizeDirective> _directiveCache = new();

        /// <summary>
        /// mod_query_user, mod_query_user_email
        /// </summary>
        Dictionary<string, string[]> Permissions = AppPermission.DynamicValues
            .GroupBy(x => x.Obj).ToDictionary(x => x.Key, x => x.Select(y => y.Field).ToArray());

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
                    var descriptor = ObjectFieldDescriptor.From(completionContext.DescriptorContext, fieldDefinition);
                    var policy = $"*_{typeName}_{fieldDefinition.Name}";
                    // this approach is not working, limited by HotChocolate's design
                    //descriptor.Authorize(policy);

                    // Get or create directive instance for this policy
                    var directive = GetOrCreateDirective(policy);

                    // Use the singleton middleware, passing the directive per invocation
                    descriptor.Use(next => async context => await _authorizeMiddleware.InvokeAsync(context, next, directive));
                    logger.LogInformation($@"成功匹配权限规则:{policy}");
                }
                foreach (var misMatch in misMatches)
                {
                    var policy = $"{typeName}_{misMatch}";
                    logger.LogWarning($@"跳过匹配权限规则:{policy}");
                }
            }
        }

        // Factory method to get or create directive instances
        private AuthorizeDirective GetOrCreateDirective(string policy)
        {
            if (!_directiveCache.TryGetValue(policy, out var directive))
            {
                directive = new AuthorizeDirective(policy);
                _directiveCache[policy] = directive;
            }
            return directive;
        }
    }
}
