using Geex.Common.Abstraction;
using JetBrains.Annotations;

namespace x_Org_x.x_Proj_x.Core.Authentication
{
    public class x_Proj_xLoginProviderEnum : LoginProviderEnum
    {
        public static x_Proj_xLoginProviderEnum x_Org_x { get; } = new x_Proj_xLoginProviderEnum(nameof(x_Org_x));
        /// <inheritdoc />
        public x_Proj_xLoginProviderEnum([NotNull] string name, [NotNull] string value) : base(name, value)
        {
        }

        /// <inheritdoc />
        public x_Proj_xLoginProviderEnum([NotNull] string value) : base(value)
        {
        }
    }
}
