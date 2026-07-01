using HotChocolate.Types.Descriptors.Definitions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class DefinitionBaseGeexFeaturesExtensions
{
    extension(DefinitionBase definition)
    {
        public GeexFeaturesAccessor GeexFeatures => new(definition);
    }
}

public readonly struct GeexFeaturesAccessor(DefinitionBase definition)
{
    public AutoBatchLoadGeexFeature AutoBatchLoad => new(definition);
}

public sealed class AutoBatchLoadGeexFeature(DefinitionBase definition)
{
    private const string ContextDataKey = "Geex.AutoBatchLoad.OperationEnabled";

    public bool? Enabled
    {
        get => definition.ContextData.TryGetValue(ContextDataKey, out var value) && value is bool enabled
            ? enabled
            : null;
        set
        {
            if (value is null)
            {
                definition.ContextData.Remove(ContextDataKey);
            }
            else
            {
                definition.ContextData[ContextDataKey] = value;
            }
        }
    }
}
