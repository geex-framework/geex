using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private IQueryable<TRelated>? _source;

        public LazyMultiQuery(Expression<Func<TRelated, bool>> lazy, Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batch, Func<IQueryable<TRelated>> sourceProvider)
        {
            this.DefaultSourceProvider = sourceProvider;
            this.Lazy = lazy;
            this.Batch = batch;
        }

        /// <inheritdoc />
        public string HashCode => this.GetHashCode().ToString();

        public IQueryable<TRelated> Value => this.Source.Where(this.Lazy).AsQueryable();
        public Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> Batch { get; set; }

        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }
        public Expression<Func<TRelated, bool>> Lazy { get; set; }

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
        private Delegate? _compiledBatch;

        Delegate ILazyQuery.BatchQuery
        {
            get
            {
                this._compiledBatch ??= Batch.CompileFast();
                return _compiledBatch;
            }
        }

        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        private Delegate? _compiledLazy;
        /// <inheritdoc />
        Delegate ILazyQuery.LazyQuery
        {
            get
            {
                this._compiledLazy ??= Lazy.CompileFast();
                return _compiledLazy;
            }
        }


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
        private IQueryable<TRelated>? _source;

        public LazySingleQuery(Expression<Func<TRelated, bool>> lazy, Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batch, Func<IQueryable<TRelated>> sourceProvider)
        {
            DefaultSourceProvider = sourceProvider;
            this.Lazy = lazy;
            this.Batch = batch;
        }

        public Lazy<TRelated> Value => new(() => this.Source.Where(this.Lazy).FirstOrDefault());
        public Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> Batch { get; set; }


        public Func<IQueryable<TRelated>> DefaultSourceProvider { get; set; }
        public Expression<Func<TRelated, bool>> Lazy { get; set; }

        public IQueryable<TRelated> Source
        {
            get => _source ?? DefaultSourceProvider();
            set => _source = value;
        }

        private Delegate? _compiledBatch;

        Delegate ILazyQuery.BatchQuery
        {
            get
            {
                this._compiledBatch ??= Batch.CompileFast();
                return _compiledBatch;
            }
        }

        Func<IQueryable> ILazyQuery.DefaultSourceProvider => DefaultSourceProvider;

        private Delegate? _compiledLazy;
        /// <inheritdoc />
        Delegate ILazyQuery.LazyQuery
        {
            get
            {
                this._compiledLazy ??= Lazy.CompileFast();
                return _compiledLazy;
            }
        }

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
