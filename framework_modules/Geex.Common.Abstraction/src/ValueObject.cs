using System;
using System.Collections.Generic;
using System.Linq;

namespace Geex.Common.Abstractions
{
    public abstract class ValueObject<T> : IValueObject where T : class
    {
        protected ValueObject(params Func<T, object>[] equalityComponents)
        {
            this.EqualityComponents = equalityComponents.Select(x => x.Invoke((this as T)!));
        }
        protected static bool EqualOperator(ValueObject<T> left, ValueObject<T> right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        protected static bool NotEqualOperator(ValueObject<T> left, ValueObject<T> right)
        {
            return !(EqualOperator(left, right));
        }

        public IEnumerable<object> EqualityComponents { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject<T>)obj;

            return this.EqualityComponents.SequenceEqual(other.EqualityComponents);
        }

        public override int GetHashCode()
        {
            return EqualityComponents
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }

        public ValueObject<T> GetCopy()
        {
            return this.MemberwiseClone() as ValueObject<T>;
        }
    }
    public interface IValueObject
    {
        IEnumerable<object> EqualityComponents { get; }
    }
}