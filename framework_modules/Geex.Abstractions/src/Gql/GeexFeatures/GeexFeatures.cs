using System;
using System.Collections.Generic;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;

namespace Geex.Gql.GeexFeatures
{
    public static class GeexFeatureKeys
    {
        internal const string AutoBatchLoadFeature = "Geex.AutoBatchLoad.Feature";
        public const string AutoBatchLoadMiddleware = "Geex.AutoBatchLoad.Middleware";
    }

    public sealed class AutoBatchLoadFeature
    {
        public AutoBatchLoadFeature(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; }
    }

    public sealed class FieldFeatures
    {
        private readonly HotChocolate.ExtensionData? _extensionData;
        private readonly IReadOnlyDictionary<string, object?>? _contextData;

        internal FieldFeatures(HotChocolate.ExtensionData contextData)
        {
            _extensionData = contextData;
        }

        internal FieldFeatures(IReadOnlyDictionary<string, object?> contextData)
        {
            _contextData = contextData;
        }

        public AutoBatchLoadFeature? AutoBatchLoadFeature
        {
            get
            {
                var contextData = (IReadOnlyDictionary<string, object?>?)_extensionData ?? _contextData;
                if (contextData is null)
                {
                    return null;
                }

                return contextData.TryGetValue(GeexFeatureKeys.AutoBatchLoadFeature, out var value) &&
                       value is AutoBatchLoadFeature feature
                    ? feature
                    : null;
            }
            set
            {
                if (_extensionData is null)
                {
                    throw new InvalidOperationException("Geex field features are read-only at runtime.");
                }

                if (value is null)
                {
                    _extensionData.Remove(GeexFeatureKeys.AutoBatchLoadFeature);
                }
                else
                {
                    _extensionData[GeexFeatureKeys.AutoBatchLoadFeature] = value;
                }
            }
        }
    }

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
