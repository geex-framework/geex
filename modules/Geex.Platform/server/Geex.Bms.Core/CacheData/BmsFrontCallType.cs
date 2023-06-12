using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;

using JetBrains.Annotations;

namespace Geex.Bms.Core.CacheData
{
    public class BmsFrontCallType : FrontendCallType
    {
        /// <inheritdoc />
        protected BmsFrontCallType([NotNull] string name, [NotNull] string value) : base(name, value)
        {
        }

        public static BmsFrontCallType CacheDataChange { get; } = new(nameof(CacheDataChange), nameof(CacheDataChange));
    }
}
