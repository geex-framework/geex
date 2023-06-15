using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;

using JetBrains.Annotations;

namespace x_Org_x.x_Proj_x.Core.CacheData
{
    public class x_Proj_xFrontCallType : FrontendCallType
    {
        /// <inheritdoc />
        protected x_Proj_xFrontCallType([NotNull] string name, [NotNull] string value) : base(name, value)
        {
        }

        public static x_Proj_xFrontCallType CacheDataChange { get; } = new(nameof(CacheDataChange), nameof(CacheDataChange));
    }
}
