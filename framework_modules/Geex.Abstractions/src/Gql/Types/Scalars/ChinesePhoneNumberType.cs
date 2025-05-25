using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Geex.Gql.Types.Scalars
{
    public class ChinesePhoneNumberType : RegexType
    {
        private const string _validationPattern = "^[1]([3-9])[0-9]{9}$";

        public ChinesePhoneNumberType()
          : this((string)"ChinesePhoneNumber", _validationPattern)
        {
        }

        public ChinesePhoneNumberType(string name, string? description = null, BindingBehavior bind = BindingBehavior.Explicit)
          : base(name, _validationPattern, description, RegexOptions.IgnoreCase | RegexOptions.Compiled, bind)
        {
        }

        protected override SerializationException CreateParseLiteralError(
          IValueNode valueSyntax)
        {
            return new SerializationException("invalid phone number", this);
        }

        protected override SerializationException CreateParseValueError(
          object runtimeValue)
        {
            return new SerializationException("invalid phone number", this);
        }
    }
}
