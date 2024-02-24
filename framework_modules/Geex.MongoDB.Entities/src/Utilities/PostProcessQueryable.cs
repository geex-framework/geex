using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    public class PostProcessQueryable<T> :
        IQueryable<T>
    {
        private readonly Func<T, T> postAction;
        private readonly IQueryable<T> innerQueryable;

        public PostProcessQueryable(IQueryable<T> innerQueryable, Func<T, T> postAction)
        {
            this.postAction = postAction;
            this.innerQueryable = innerQueryable;
        }

        Expression IQueryable.Expression => this.innerQueryable.Expression;

        Type IQueryable.ElementType => typeof(T);

        IQueryProvider IQueryable.Provider => new PostProcessQueryProvider<T>(this.innerQueryable.Provider, postAction);

        public IEnumerator<T> GetEnumerator() => this.innerQueryable.AsEnumerable().Select(x => postAction.Invoke(x)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public class PostProcessQueryProvider<T> : IQueryProvider
    {
        private readonly IQueryProvider innerProvider;
        private readonly Func<T, T> postAction;

        public PostProcessQueryProvider(IQueryProvider innerProvider, Func<T, T> postAction)
        {
            this.innerProvider = innerProvider;
            this.postAction = postAction;
        }

        public IQueryable CreateQuery(Expression expression) => this.CreateQuery<T>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(TElement).IsAssignableFrom(typeof(T)) && innerProvider is not PostProcessQueryProvider<T>)
            {
                return new PostProcessQueryable<TElement>(innerProvider.CreateQuery<TElement>(expression), (x) => (TElement)(object)this.postAction.Invoke((T)(object)x));
            }
            return innerProvider.CreateQuery<TElement>(expression);
        }


        object IQueryProvider.Execute(Expression expression) => (object)this.innerProvider.Execute<T>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            var result = this.innerProvider.Execute<TResult>(expression);
            if (result is T entity)
            {
                return (TResult)(object)postAction.Invoke(entity);
            }
            return result;
        }
    }


}