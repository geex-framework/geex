using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Entities.Utilities
{

    internal class QueryPartsExpressionVisitor<TEntity, TResult> : ExpressionVisitor
    {
        #region static query methods

        private static MethodInfo _queryableWhereMethod;
        private static MethodInfo QueryableWhereMethod
        {
            get
            {
                if (_queryableWhereMethod != default)
                {
                    return _queryableWhereMethod;
                }
                Expression<Func<IQueryable<TEntity>, IQueryable<TEntity>>> a = x => x.Where(y => true);
                _queryableWhereMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableWhereMethod;
            }
        }
        static MethodInfo _queryableOfTypeMethod;
        private static MethodInfo QueryableOfTypeMethod
        {
            get
            {
                if (_queryableOfTypeMethod != default)
                {
                    return _queryableOfTypeMethod;
                }
                Expression<Func<IQueryable<TEntity>, IQueryable<TEntity>>> a = x => x.OfType<TEntity>();
                _queryableOfTypeMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableOfTypeMethod;
            }
        }
        private static MethodInfo _queryableFirstMethod;
        private static MethodInfo QueryableFirstMethod
        {
            get
            {
                if (_queryableFirstMethod != default)
                {
                    return _queryableFirstMethod;
                }
                Expression<Func<IQueryable<TEntity>, TEntity>> a = x => x.First();
                _queryableFirstMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableFirstMethod;
            }
        }
        private static MethodInfo _queryableFirstOrDefaultMethod;
        private static MethodInfo QueryableFirstOrDefaultMethod
        {
            get
            {
                if (_queryableFirstOrDefaultMethod != default)
                {
                    return _queryableFirstOrDefaultMethod;
                }
                Expression<Func<IQueryable<TEntity>, TEntity>> a = x => x.FirstOrDefault();
                _queryableFirstOrDefaultMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableFirstOrDefaultMethod;
            }
        }
        private static MethodInfo _queryableSingleMethod;
        private static MethodInfo QueryableSingleMethod
        {
            get
            {
                if (_queryableSingleMethod != default)
                {
                    return _queryableSingleMethod;
                }
                Expression<Func<IQueryable<TEntity>, TEntity>> a = x => x.Single();
                _queryableSingleMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableSingleMethod;
            }
        }
        private static MethodInfo _queryableSingleOrDefaultMethod;
        private int selectIndex;

        private static MethodInfo QueryableSingleOrDefaultMethod
        {
            get
            {
                if (_queryableSingleOrDefaultMethod != default)
                {
                    return _queryableSingleOrDefaultMethod;
                }
                Expression<Func<IQueryable<TEntity>, TEntity>> a = x => x.SingleOrDefault();
                _queryableSingleOrDefaultMethod = a.Body.As<MethodCallExpression>().Method.GetGenericMethodDefinition();
                return _queryableSingleOrDefaultMethod;
            }
        }

        #endregion

        //there must be only one instance of parameter expression for each parameter
        //there is one so one passed here
        public QueryPartsExpressionVisitor()
        {
        }

        public Expression PreSelectExpression { get; set; }
        public Expression PostSelectExpression { get; set; }
        public MethodCallExpression ExecuteExpression { get; set; }
        //public Expression ExecuteFilterExpression { get; set; }
        /// <inheritdoc />
        public override Expression Visit(Expression node)
        {
            var stages = new List<Expression>();
            if (node is MethodCallExpression reversedStage)
            {
                stages.Add(reversedStage);
                while (reversedStage.Arguments.FirstOrDefault() is MethodCallExpression next)
                {
                    stages.Add(next);
                    reversedStage = next;
                }

                var root = reversedStage.Arguments.FirstOrDefault();
                var rootType = typeof(TEntity).GetRootBsonClassMap().ClassType;
                if (rootType != typeof(TEntity))
                {
                    root = Expression.Call(QueryableOfTypeMethod.MakeGenericMethod(typeof(TEntity)), root);
                }
                this.ItemType = root.Type.GenericTypeArguments[0];
                stages.Add(root);
                stages.Reverse();
                // 跳过root变换
                for (var i = 1; i < stages.Count; i++)
                {
                    // 统一每一个stage的参数类型到具体类, 接口转换实现类
                    if (stages[i] is MethodCallExpression mce)
                    {
                        var genericArgs = mce.Method.GetGenericArguments();
                        if (genericArgs.Any(x => x.IsAssignableFrom(ItemType)))
                        {
                            MethodInfo method;
                            if (mce.Method.IsGenericMethod)
                                method = mce.Method.GetGenericMethodDefinition().MakeGenericMethod(mce.Method
                                    .GetGenericArguments().Select(x =>
                                    {
                                        if (x.IsAssignableFrom(ItemType))
                                            return ItemType;
                                        else
                                            return x;
                                    })
                                    .ToArray());
                            else
                                method = mce.Method;
                            var source = stages[i - 1];
                            var expression = mce.Arguments.ElementAtOrDefault(1);
                            if (expression == null)
                            {
                                stages[i] = Expression.Call(method, source);

                            }
                            else
                            {
                                if (expression is UnaryExpression unary && unary?.Operand is LambdaExpression lambda && lambda.Parameters.Any(x => x.Type.IsAssignableFrom(ItemType)))
                                {
                                    expression = Expression.MakeUnary(unary.NodeType, lambda.CastParamType(this.ItemType), unary.Type);
                                }
                                stages[i] = Expression.Call(method, source, expression);
                            }
                        }
                    }
                }
            }
            else
            {
                stages = new List<Expression>() { node };
            }

            this.selectIndex = stages.FindIndex(x => x is MethodCallExpression mce && mce.Method.Name is nameof(Queryable.Select) or nameof(Queryable.SelectMany) or nameof(Queryable.Skip) or nameof(Queryable.Take));

            this.ValidateStages(stages);
            var lastStage = stages.LastOrDefault();
            if (lastStage is MethodCallExpression mce1 && mce1.Method.Name is nameof(Queryable.Count) or nameof(Queryable.LongCount) or nameof(Queryable.Any) or nameof(Queryable.Sum) or nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                            or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault))
            {
                if (mce1.Method.Name is nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                            or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault))
                {
                    // 将执行过滤参数转为Where过滤参数 .First(x=>true) => .Where(x=>true).First()
                    var lastStageFilter = mce1.Arguments.ElementAtOrDefault(1);
                    if (lastStageFilter is UnaryExpression unary && unary?.Operand is LambdaExpression lambda && lambda.Parameters.Any(x => x.Type.IsAssignableFrom(ItemType)))
                    {
                        lastStageFilter = Expression.Call(QueryableWhereMethod.MakeGenericMethod(ItemType), mce1.Arguments[0], lambda.CastParamType(this.ItemType));
                        mce1 = mce1.Method.Name switch
                        {
                            nameof(Queryable.First) => Expression.Call(QueryableFirstMethod.MakeGenericMethod(ItemType), mce1.Arguments[0]),
                            nameof(Queryable.FirstOrDefault) => Expression.Call(QueryableFirstOrDefaultMethod.MakeGenericMethod(ItemType), mce1.Arguments[0]),
                            nameof(Queryable.Single) => Expression.Call(QueryableSingleMethod.MakeGenericMethod(ItemType), mce1.Arguments[0]),
                            nameof(Queryable.SingleOrDefault) => Expression.Call(QueryableSingleOrDefaultMethod.MakeGenericMethod(ItemType), mce1.Arguments[0]),
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        stages = stages.SkipLast(1).Concat(new[] { lastStageFilter, mce1 }).ToList();
                    }
                }

                if (selectIndex == -1)
                {
                    PreSelectExpression = stages.ElementAtOrDefault(stages.Count - 2);
                }
                else
                {
                    PreSelectExpression = stages[selectIndex - 1];
                    if (stages.Count - 2 > selectIndex)
                    {
                        PostSelectExpression = stages.ElementAtOrDefault(stages.Count - 2);
                    }
                }
                ExecuteExpression = mce1;
            }
            else
            {
                if (selectIndex == -1)
                {
                    PreSelectExpression = lastStage;
                }
                else
                {
                    PreSelectExpression = stages[selectIndex - 1];
                    if (stages.Count - 1 > selectIndex)
                    {
                        PostSelectExpression = stages.ElementAtOrDefault(stages.Count - 1);
                    }
                    ExecuteExpression = stages.ElementAtOrDefault(selectIndex)?.As<MethodCallExpression>();
                }
            }

            return node;
        }

        private void ValidateStages(List<Expression> stages)
        {
        }

        public Type ItemType { get; set; }
    }

    public class EntityEqualityComparer<T> : IEqualityComparer<T> where T : IEntityBase
    {
        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>
        /// <see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
        public bool Equals(T x, T y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is <see langword="null" />.</exception>
        public int GetHashCode(T obj)
        {
            return obj.Id.GetHashCode();
        }

        public static IEqualityComparer<T> Instance { get; } = new EntityEqualityComparer<T>();
    }
}
