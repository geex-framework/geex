//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//using HotChocolate.Language;
//using HotChocolate.Types;

//namespace Geex.Common.Abstraction.Gql.Types.Scalars
//{
//    public class Base64StringType : ScalarType<byte[], StringValueNode>
//    {
//        /// <inheritdoc />
//        public Base64StringType(BindingBehavior bind = BindingBehavior.Explicit) : base("Base64String", bind)
//        {
//        }

//        /// <inheritdoc />
//        public override IValueNode ParseResult(object? resultValue)
//        {
//            throw new NotImplementedException();
//        }

//        /// <inheritdoc />
//        protected override byte[] ParseLiteral(StringValueNode valueSyntax)
//        {
//            return Convert.FromBase64String(valueSyntax.Value);
//        }

//        /// <inheritdoc />
//        protected override StringValueNode ParseValue(byte[] runtimeValue)
//        {
//            return new StringValueNode(Convert.ToBase64String(runtimeValue));
//        }
//    }
//}
