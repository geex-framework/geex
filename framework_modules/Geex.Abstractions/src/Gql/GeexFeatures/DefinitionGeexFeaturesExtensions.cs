using Geex.Gql.GeexFeatures;

using HotChocolate.Types.Descriptors.Definitions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class DefinitionGeexFeaturesExtensions
{
    extension(DefinitionBase definition)
    {
        public GeexFeaturesAccessor GeexFeatures => new(definition);
    }
}
