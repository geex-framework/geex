using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Gql;
using Geex.Gql.Types;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Extensions.AuditLogs.Utils
{
    public class AuditLogsTypeInterceptor : TypeInterceptor
    {
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

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                if (ToBePatchedBuiltInOperations.TryGetValue(objectTypeDefinition.RuntimeType.Name, out var fieldNames))
                {
                    var toBePatchedFields = objectTypeDefinition.Fields.Where(x => fieldNames.Contains(x.Name.ToLowerInvariant()));
                    foreach (var bePatchedField in toBePatchedFields)
                    {
                        bePatchedField.Directives.Add(new DirectiveDefinition(new DirectiveNode("audit")));
                    }
                }
            }
            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
