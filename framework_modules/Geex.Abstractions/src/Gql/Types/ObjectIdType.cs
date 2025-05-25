using System;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Bson;

namespace Geex.Gql.Types
{
    public class ObjectIdType : ScalarType<ObjectId>
    {
        public ObjectIdType() : base("ObjectId")
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object? ParseLiteral(IValueNode valueSyntax)
        {
            if (valueSyntax == null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is StringValueNode stringLiteral)
            {
                return ObjectId.Parse(stringLiteral.Value);
            }

            throw new SerializationException("", new ObjectIdType());

        }

        public override IValueNode ParseValue(object value)
        {
            if (value is ObjectId s)
            {
                return new StringValueNode(s.ToString());
            }

            throw new SerializationException("", new ObjectIdType());
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            return this.ParseValue(resultValue);
        }

        public override object Serialize(object value)
        {
            if (value is ObjectId s)
            {
                return s;
            }

            throw new SerializationException("", new ObjectIdType());
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            try
            {
                resultValue = this.Serialize(runtimeValue);
                return true;
            }
            catch (Exception)
            {
                resultValue = null;
                return false;
            }
        }

        public override bool TryDeserialize(object? serialized, out object value)
        {
            if (serialized is string str)
            {
                value = ObjectId.Parse(str);
                return true;
            }

            if (serialized is ObjectId)
            {
                value = serialized;
                return true;
            }

            value = null;
            return false;
        }
    }
}
