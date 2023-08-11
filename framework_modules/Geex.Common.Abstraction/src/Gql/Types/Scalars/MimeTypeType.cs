using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Geex.Common.Identity.Api.GqlSchemas.Roles.Types;

using HotChocolate.Language;
using HotChocolate.Types;

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Geex.Common.Abstraction.Gql.Types.Scalars
{
    public class MimeTypeType : ScalarType<MediaType>
    {
        private static Regex _ValidationRegex = new Regex(@"^\w+/[\w|\.|\-|\+]+$", RegexOptions.Compiled);
        private Regex _validationRegex => _ValidationRegex;

        /// <inheritdoc />
        public MimeTypeType(string name, BindingBehavior bind = BindingBehavior.Explicit) : base(name, bind)
        {
        }

        /// <inheritdoc />
        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            return valueSyntax is StringValueNode stringValueNode && this._validationRegex.IsMatch(stringValueNode.Value);
        }

        /// <inheritdoc />
        public override object? ParseLiteral(IValueNode valueSyntax)
        {
            return new MediaType((valueSyntax as StringValueNode).Value);
        }

        /// <inheritdoc />
        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is MediaType mediaType)
            {
                return new StringValueNode(mediaType.ToString());
            }
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
