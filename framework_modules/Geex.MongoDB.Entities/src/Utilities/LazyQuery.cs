using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace MongoDB.Entities.Utilities
{
    public interface ILazyQuery
    {
        Delegate BatchQuery { get; }
        Func<IQueryable> DefaultSourceProvider { get; }
        Delegate LazyQuery { get; }
        object Value { get; }
        IQueryable Source { get; set; }
    }
    internal interface ILazyMultipleQuery : ILazyQuery
    {
        IQueryable Value { get; }
        object ILazyQuery.Value => Value;
    }
    internal interface ILazySingleQuery : ILazyQuery
    {
        object ILazyQuery.Value => Value;
    }
    public class LazyMultiQuery<T, TRelated> : ILazyMultipleQuery, IQueryable<TRelated> where TRelated : IEntityBase where T : IEntityBase
    {
        private IQueryable<TRelated> _source;

        public LazyMultiQuery(Expression<Func<TRelated, bool>> lazy, Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batch, Func<IQueryable<TRelated>> sourceProvider)
        {
            this.DefaultSourceProvider = sourceProvider;
            this.Lazy = lazy.CompileFast();
            this.Batch = batch.CompileFast();
        }

        /// <inheritdoc />
        public string HashCode => this.GetHashCode().ToString();

        public IQueryable<TRelated> Value => this.Source.Where(this.Lazy).ToList().AsQueryable();
        public Func<IQueryable<T>, Expression<Func<TRelated, bool>>> Batch { get; set; }

        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }
        public Func<TRelated, bool> Lazy { get; set; }

        public IQueryable<TRelated> Source
        {
            get => _source ?? DefaultSourceProvider();
            set => _source = value;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.Value).GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<TRelated> GetEnumerator()
        {
            return this.Value.GetEnumerator();
        }

        /// <inheritdoc />
        IQueryable ILazyMultipleQuery.Value => Value;

        Delegate ILazyQuery.BatchQuery => Batch;

        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        /// <inheritdoc />
        Delegate ILazyQuery.LazyQuery => Lazy;


        /// <inheritdoc />
        IQueryable ILazyQuery.Source
        {
            get => Source;
            set => Source = (IQueryable<TRelated>)value;
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
            this.Lazy = lazy.CompileFast();
            this.Batch = batch.CompileFast();
        }

        public Lazy<TRelated> Value => new(() => this.Source.Where(this.Lazy).FirstOrDefault());
        public Func<IQueryable<T>, Expression<Func<TRelated, bool>>> Batch { get; set; }


        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }
        public Func<TRelated, bool> Lazy { get; set; }

        public IQueryable<TRelated> Source
        {
            get => _source ?? DefaultSourceProvider();
            set => _source = value;
        }

        Delegate ILazyQuery.BatchQuery => Batch;

        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        /// <inheritdoc />
        Delegate ILazyQuery.LazyQuery => Lazy;

        /// <inheritdoc />
        IQueryable ILazyQuery.Source
        {
            get => Source;
            set => Source = (IQueryable<TRelated>)value;
        }

        /// <inheritdoc />
        object ILazyQuery.Value => Value;
    }
}
