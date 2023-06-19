using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Castle.Core;

using Microsoft.Extensions.DependencyInjection;

namespace MongoDB.Entities.Utilities
{
    public interface ILazyQuery
    {
        Func<IQueryable> DefaultSourceProvider { get; }
        IQueryable Source { get; set; }
        Expression LazyQuery { get; }
        Expression BatchQuery { get; }
        object Value { get; }
    }
    internal interface ILazyMultipleQuery : ILazyQuery
    {
        object ILazyQuery.Value => Value;
        IQueryable Value { get; }
    }
    internal interface ILazySingleQuery : ILazyQuery
    {
        object ILazyQuery.Value => Value;
        IEntityBase Value { get; }
    }
    public class LazyMultiQuery<T, TRelated> : ILazyMultipleQuery, IQueryable<TRelated> where TRelated : IEntityBase where T : IEntityBase
    {
        private IQueryable<TRelated> _source;


        public LazyMultiQuery(Expression<Func<TRelated, bool>> lazy, Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batch, Func<IQueryable<TRelated>> sourceProvider)
        {
            this.DefaultSourceProvider = sourceProvider;
            this.Lazy = lazy;
            this.Batch = batch;
        }

        /// <inheritdoc />
        public string HashCode => this.GetHashCode().ToString();
        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }


        /// <inheritdoc />
        IQueryable ILazyQuery.Source
        {
            get => Source;
            set => Source = (IQueryable<TRelated>)value;
        }

        public IQueryable<TRelated> Source
        {
            get => _source ?? DefaultSourceProvider();
            set => _source = value;
        }

        /// <inheritdoc />
        IQueryable ILazyMultipleQuery.Value => Value;

        public IQueryable<TRelated> Value => this.Source.Where(this.Lazy).ToList().AsQueryable();

        /// <inheritdoc />
        Expression ILazyQuery.LazyQuery => Lazy;
        public Expression<Func<TRelated, bool>> Lazy { get; set; }

        Expression ILazyQuery.BatchQuery => Batch;
        public Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> Batch { get; set; }

        /// <inheritdoc />
        public IEnumerator<TRelated> GetEnumerator()
        {
            return this.Value.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.Value).GetEnumerator();
        }

        /// <inheritdoc />
        public Type ElementType => this.Value.ElementType;

        /// <inheritdoc />
        public Expression Expression => this.Value.Expression;

        /// <inheritdoc />
        public IQueryProvider Provider => this.Value.Provider;
    }

    public class LazySingleQuery<T, TRelated> : ILazySingleQuery where TRelated : IEntityBase where T : IEntityBase
    {
        private IQueryable<TRelated> _source;

        public LazySingleQuery(Expression<Func<TRelated, bool>> lazy, Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batch, Func<IQueryable<TRelated>> sourceProvider)
        {
            DefaultSourceProvider = sourceProvider;
            this.Lazy = lazy;
            this.Batch = batch;
        }
        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }

        /// <inheritdoc />
        IQueryable ILazyQuery.Source
        {
            get => Source;
            set => Source = (IQueryable<TRelated>)value;
        }

        public IQueryable<TRelated> Source
        {
            get => _source ?? DefaultSourceProvider();
            set => _source = value;
        }

        /// <inheritdoc />
        IEntityBase ILazySingleQuery.Value => (IEntityBase)Value;

        public TRelated Value => this.Source.FirstOrDefault(this.Lazy);

        /// <inheritdoc />
        Expression ILazyQuery.LazyQuery => Lazy;
        public Expression<Func<TRelated, bool>> Lazy { get; set; }

        Expression ILazyQuery.BatchQuery => Batch;
        public Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> Batch { get; set; }
    }
}
