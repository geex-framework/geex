using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Geex.Gql.Types.Scalars
{
    public abstract class RegexStringType<TRuntimeType> : ScalarType<TRuntimeType, StringValueNode>
    {
        protected internal const int DefaultRegexTimeoutInMs = 200;
        private readonly Regex _validationRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HotChocolate.Types.RegexStringType" /> class.
        /// </summary>
        public RegexStringType(
          string name,
          string pattern,
          string? description = null,
          RegexOptions regexOptions = RegexOptions.Compiled,
          BindingBehavior bind = BindingBehavior.Explicit)
          : this(name, new Regex(pattern, regexOptions, TimeSpan.FromMilliseconds(200.0)), description, bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HotChocolate.Types.RegexStringType" /> class.
        /// </summary>
        public RegexStringType(string name, Regex regex, string? description = null, BindingBehavior bind = BindingBehavior.Explicit)
          : base(name, bind)
        {
            this.Description = description;
            this._validationRegex = regex;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(TRuntimeType runtimeValue) => this._validationRegex.IsMatch(runtimeValue.ToString());

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax) => this._validationRegex.IsMatch(valueSyntax.Value);

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue == null)
            {
                resultValue = (object)null;
                return true;
            }
            if (runtimeValue is string input && this._validationRegex.IsMatch(input))
            {
                resultValue = (object)input;
                return true;
            }
            resultValue = (object)null;
            return false;
        }

        /// <inheritdoc />
        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue == null)
            {
                runtimeValue = (object)null;
                return true;
            }
            if (resultValue is string input && this._validationRegex.IsMatch(input))
            {
                runtimeValue = (object)input;
                return true;
            }
            runtimeValue = (object)null;
            return false;
        }
    }
}
