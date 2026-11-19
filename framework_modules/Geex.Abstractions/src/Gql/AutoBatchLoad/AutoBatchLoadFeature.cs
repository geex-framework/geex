using System;

using HotChocolate;

namespace Geex.Gql.AutoBatchLoad;

public readonly struct AutoBatchLoadFeatureConfig(bool isEnabled) : FeatureConfig
{
  public bool IsEnabled { get; } = isEnabled;
}

public static class AutoBatchLoadFeature
{
  extension(GeexFeatures features)
  {
    public AutoBatchLoadFeatureConfig? AutoBatchLoad
    {
      get => features.GetFeatureConfig<FeatureConfig>(GeexFeature.AutoBatchLoad) is AutoBatchLoadFeatureConfig config
        ? config
        : null;
      set => features.SetFeatureConfig(GeexFeature.AutoBatchLoad, value);
    }
  }
}
