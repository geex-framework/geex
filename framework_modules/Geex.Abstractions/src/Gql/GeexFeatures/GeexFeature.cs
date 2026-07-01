using Geex;

namespace Geex.Gql.GeexFeatures;

public class GeexFeature : Enumeration<GeexFeature>
{
    public static GeexFeature AutoBatchLoad { get; } =
        FromNameAndValue(nameof(AutoBatchLoad), nameof(AutoBatchLoad));
}
