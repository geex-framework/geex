using Geex.Gql.GeexFeatures;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;

namespace Geex.Gql.AutoBatchLoad
{
    public static class GeexFieldExtensions
    {
        extension(IOutputField receiver)
        {
            public FieldFeatures GeexFeatures => new(receiver.ContextData);

            public bool AutoBatchLoadEnabled =>
                receiver.GeexFeatures.AutoBatchLoadFeature?.Enabled is true;

            public bool HasOffsetPaging =>
                receiver.Type.NamedType() is IPageType;
        }

        extension(ObjectFieldDefinition receiver)
        {
            public FieldFeatures GeexFeatures => new(receiver.ContextData);

            public bool AutoBatchLoadEnabled =>
                receiver.GeexFeatures.AutoBatchLoadFeature?.Enabled is true;
        }
    }
}
