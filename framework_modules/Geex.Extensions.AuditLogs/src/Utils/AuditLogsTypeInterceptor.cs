using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Geex.Extensions.Authentication.Gql;
using Geex.Gql;
using Geex.Gql.Types;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Extensions.AuditLogs.Utils
{
    public class AuditLogsTypeInterceptor : TypeInterceptor
    {
        private readonly AuditLevel _defaultAuditLevel;

        public AuditLogsTypeInterceptor(AuditLogsModuleOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (!Enum.IsDefined(options.DefaultAuditLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(options.DefaultAuditLevel));
            }
            _defaultAuditLevel = options.DefaultAuditLevel;
        }

        public static List<string> AuthenticationMutationMembers = typeof(AuthenticationMutation).GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .IntersectBy([
                nameof(AuthenticationMutation.Authenticate),
                nameof(AuthenticationMutation.FederateAuthenticate),
                nameof(AuthenticationMutation.CancelAuthentication)
            ], x => x.Name)
            .Select(x => x.Name.ToLowerInvariant())
            .ToList();
        public static Dictionary<string, List<string>> ToBePatchedBuiltInOperations = new()
        {
            { nameof(Mutation), [..AuthenticationMutationMembers] },
        };

        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition &&
                objectTypeDefinition.Name is nameof(Query) or nameof(Mutation) or nameof(Subscription))
            {
                foreach (var field in objectTypeDefinition.Fields)
                {
                    if (field.Ignore || field.IsIntrospectionField || field.Name is "_" ||
                        field.Name.StartsWith("__", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    SetAuditDirective(field, IsEnabled(completionContext, objectTypeDefinition, field));
                }
            }
            base.OnBeforeCompleteType(completionContext, definition);
        }

        private bool IsEnabled(ITypeCompletionContext context, ObjectTypeDefinition root, ObjectFieldDefinition field)
        {
            if (field.ContextData.TryGetValue(AuditLogExtensions.FieldAuditKey, out var explicitValue) &&
                explicitValue is bool enabled)
            {
                return enabled;
            }
            if (field.Directives.Any(IsAuditDirective) ||
                ToBePatchedBuiltInOperations.TryGetValue(root.Name, out var builtInFields) &&
                builtInFields.Contains(field.Name.ToLowerInvariant()))
            {
                return true;
            }

            var declaringType = field.ContextData.TryGetValue(GeexTypeInterceptor.RootFieldDeclaringTypeKey, out var origin)
                ? origin as Type
                : field.Member?.DeclaringType;
            var level = AuditLogExtensions.GetTypeAuditLevel(context.DescriptorContext, declaringType) ?? _defaultAuditLevel;
            return level == AuditLevel.All || level == AuditLevel.MutationOnly && root.Name == nameof(Mutation);
        }

        internal static void SetAuditDirective(ObjectFieldDefinition field, bool enabled)
        {
            foreach (var directive in field.Directives.Where(IsAuditDirective).ToArray())
            {
                field.Directives.Remove(directive);
            }
            if (enabled)
            {
                field.Directives.Add(new DirectiveDefinition(new DirectiveNode(AuditDirectiveType.DirectiveName)));
            }
        }

        private static bool IsAuditDirective(DirectiveDefinition directive) =>
            directive.Value is AuditDirectiveType ||
            directive.Value is DirectiveNode node && node.Name.Value == AuditDirectiveType.DirectiveName;
    }
}
