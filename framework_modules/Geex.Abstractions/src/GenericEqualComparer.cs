using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AutoEquality;

namespace Geex.Abstractions
{
    public sealed class GenericEqualityComparer<T> : AutoEqualityComparerBase<T> where T : class
    {
        public GenericEqualityComparer()
        {
        }

        public new GenericEqualityComparer<T> With<TProperty>(
            Expression<Func<T, TProperty>> withProperty,
            IEqualityComparer<TProperty> comparer = null)
        {
            base.With<TProperty>(withProperty, comparer);
            return this;
        }

        public new GenericEqualityComparer<T> With<TProperty>(
            Expression<Func<T, IEnumerable<TProperty>>> withProperty,
            bool inAnyOrder = false,
            IEqualityComparer<TProperty> comparer = null)
        {
            base.With<TProperty>(withProperty, inAnyOrder, comparer);
            return this;
        }

        public new GenericEqualityComparer<T> WithAll()
        {
            base.WithAll();
            return this;
        }

        public new GenericEqualityComparer<T> WithComparer<TComparer>(IEqualityComparer<TComparer> typeComparer)
        {
            base.WithComparer<TComparer>(typeComparer);
            return this;
        }

        public new GenericEqualityComparer<T> Without<TProperty>(Expression<Func<T, TProperty>> withoutProperty)
        {
            base.Without<TProperty>(withoutProperty);
            return this;
        }

        public new GenericEqualityComparer<T> WithoutAll()
        {
            base.WithoutAll();
            return this;
        }

        public new GenericEqualityComparer<T> WithoutComparer(Type comparerType)
        {
            base.WithoutComparer(comparerType);
            return this;
        }
    }
}
