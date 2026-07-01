using System;
using System.Collections.Generic;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.GeexFeatures;

public readonly struct AutoBatchLoadFeatureConfig(bool isEnabled)
{
    public bool IsEnabled { get; } = isEnabled;
}

public struct GeexFeaturesAccessor
{
    private readonly IReadOnlyDictionary<string, object?> _contextData;

    internal GeexFeaturesAccessor(DefinitionBase definition) => _contextData = definition.ContextData;

    internal GeexFeaturesAccessor(IOutputField outputField) => _contextData = outputField.ContextData;

    public AutoBatchLoadFeatureConfig? AutoBatchLoad
    {
        get
        {
            if (_contextData is null ||
                !_contextData.TryGetValue(GeexFeature.AutoBatchLoad, out var value) ||
                value is not AutoBatchLoadFeatureConfig config)
            {
                return null;
            }

            return config;
        }
        set
        {
            if (_contextData is not ExtensionData definitionBaseData)
            {
                throw new InvalidOperationException(
                    "Geex feature configuration can only be set during schema definition.");
            }

            definitionBaseData[GeexFeature.AutoBatchLoad] = value!;
        }
    }
}
