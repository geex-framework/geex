using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Authorization.Core.Utils
{
    /// <summary>
    /// Type interceptor for handling authorization on fields
    /// </summary>
    public class AuthorizationTypeInterceptor : TypeInterceptor
    {
        static Type AuthorizeMiddlewareType = typeof(AuthorizeDirective).Assembly.GetType("HotChocolate.Authorization.AuthorizeMiddleware", true);
        private static readonly ConstructorInfo Ctor = AuthorizeMiddlewareType.GetConstructor(new[] { typeof(FieldDelegate), typeof(AuthorizeDirective) });
        private static readonly MethodInfo Invoke = AuthorizeMiddlewareType.GetMethod("InvokeAsync");
        /// <summary>
        /// mod_query_user, mod_query_user_email
        /// </summary>
        Dictionary<string, string[]> Permissions = AppPermission.List
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
                    var policy = $"{typeName}_{fieldDefinition.Name}";
                    //descriptor.Authorize(policy);
                    //if (!fieldDefinition.Directives.Any(x => (x.Value is AuthorizeDirective)))
                    //{
                    //    fieldDefinition.AddDirective(new AuthorizeDirective(policy), completionContext.TypeInspector);
                    //}
                    var directive = new AuthorizeDirective(policy);
                    //fieldDefinition.MiddlewareDefinitions.Add(new FieldMiddlewareDefinition(next => async context =>
                    //    await new AuthorizeMiddleware(next, directive).InvokeAsync(context).ConfigureAwait(true)));
                    descriptor.Use(next => async context =>
                        await new AuthorizeMiddleware(next, directive).InvokeAsync(context));
                    logger.LogInformation($@"成功匹配权限规则:{policy}");
                }
                foreach (var misMatch in misMatches)
                {
                    var policy = $"{typeName}_{misMatch}";
                    logger.LogWarning($@"跳过匹配权限规则:{policy}");
                }
            }
        }
    }
}
