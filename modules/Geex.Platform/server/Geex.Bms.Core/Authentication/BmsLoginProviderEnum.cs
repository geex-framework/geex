using Geex.Common.Abstraction;
using JetBrains.Annotations;

namespace Geex.Bms.Core.Authentication
{
    public class BmsLoginProviderEnum : LoginProviderEnum
    {
        public static BmsLoginProviderEnum Geex { get; } = new BmsLoginProviderEnum(nameof(Geex));
        /// <inheritdoc />
        public BmsLoginProviderEnum([NotNull] string name, [NotNull] string value) : base(name, value)
        {
        }

        /// <inheritdoc />
        public BmsLoginProviderEnum([NotNull] string value) : base(value)
        {
        }
    }
}
