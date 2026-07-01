using System;
using System.Collections.Generic;

using HotChocolate;

namespace Geex.Gql;

public interface FeatureConfig
{
}

public class GeexFeature : Enumeration<GeexFeature>
{
    public static GeexFeature AutoBatchLoad { get; } =
        FromNameAndValue(nameof(AutoBatchLoad), nameof(AutoBatchLoad));
}

public readonly struct GeexFeatures(IReadOnlyDictionary<string, object?> contextData)
{
  private IReadOnlyDictionary<string, object?> ContextData { get; } = contextData;
  public T? GetFeatureConfig<T>(GeexFeature feature) where T : FeatureConfig
  {
    return ContextData.TryGetValue(feature, out var value) ? (T)value : default;
  }
  public void SetFeatureConfig<T>(GeexFeature feature, T value) 
  {
    if (ContextData is not ExtensionData definitionBaseData)
    {
      throw new InvalidOperationException(
          "Geex feature configuration can only be set during schema definition.");
    }
    definitionBaseData[feature] = value!;
  }
}
