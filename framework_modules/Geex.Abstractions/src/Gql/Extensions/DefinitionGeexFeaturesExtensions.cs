using Geex.Gql;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions
{
  public static class DefinitionGeexFeaturesExtensions
  {
    extension(DefinitionBase definition)
    {
      public GeexFeatures GeexFeatures => new(definition.ContextData);
    }
  }
}
