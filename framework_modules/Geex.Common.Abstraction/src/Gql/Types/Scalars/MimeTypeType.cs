using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Types;

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Geex.Common.Abstraction.Gql.Types.Scalars
{
    public class MimeTypeType : ScalarType<MediaType, StringValueNode>
    {
        private static Regex _ValidationRegex = new Regex(@"^\w+/[\w|\.|\-|\+]+$", RegexOptions.Compiled);
        private Regex _validationRegex => _ValidationRegex;

        /// <inheritdoc />
        public MimeTypeType() : base("MimeType")
        {
            Description = "mime type, e.g. application/json";
        }

        /// <inheritdoc />
        protected override MediaType ParseLiteral(StringValueNode valueSyntax)
        {
            return new MediaType((valueSyntax).Value);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(MediaType runtimeValue)
        {
            return new StringValueNode(runtimeValue.ToString());
        }

        /// <inheritdoc />
        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is MediaType mediaType)
            {
                return new StringValueNode(mediaType.ToString());
            }
            return new StringValueNode(resultValue.ToString());
        }
    }
}
