using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Extensions.Favorites.Gql;

internal sealed class FavoriteTitleFieldInterceptor : TypeInterceptor
{
    internal const string DirectProperty = "Geex.Favorites.DirectTitleProperty";

    public override void OnBeforeCompleteType(ITypeCompletionContext context, DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition type) return;
        foreach (var field in type.Fields)
        {
            if (field.Member is PropertyInfo property && property.PropertyType == typeof(string) &&
                property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0 &&
                (field.ResolverMember == null || field.ResolverMember == property) &&
                field.Resolver == null && field.PureResolver == null && field.MiddlewareDefinitions.Count == 0)
                field.ContextData[DirectProperty] = field;
        }
    }
}
