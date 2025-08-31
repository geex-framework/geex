using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MongoDB.Bson.Serialization;

using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Entities.Utilities
{
    static class Q
    {
        internal const string Where = nameof(Queryable.Where);

        internal const string Select = nameof(Queryable.Select);
        internal const string GroupBy = nameof(Queryable.GroupBy);
        internal const string SelectMany = nameof(Queryable.SelectMany);

        internal const string Count = nameof(Queryable.Count);
        internal const string LongCount = nameof(Queryable.LongCount);
        internal const string Any = nameof(Queryable.Any);

        internal const string First = nameof(Queryable.First);
        internal const string FirstOrDefault = nameof(Queryable.FirstOrDefault);
        internal const string Single = nameof(Queryable.Single);
        internal const string SingleOrDefault = nameof(Queryable.SingleOrDefault);

        internal const string Sum = nameof(Queryable.Sum);
        internal const string Min = nameof(Queryable.Min);
        internal const string MinBy = nameof(Queryable.MinBy);
        internal const string Average = nameof(Queryable.Average);
        internal const string Max = nameof(Queryable.Max);
        internal const string MaxBy = nameof(Queryable.MaxBy);

        internal const string Skip = nameof(Queryable.Skip);
        internal const string Take = nameof(Queryable.Take);

        internal const string Distinct = nameof(Queryable.Distinct);
        internal const string DistinctBy = nameof(Queryable.DistinctBy);
    }
    internal class QueryPartsExpressionVisitor<TEntity, TResult> : ExpressionVisitor
    {
        public bool HasGroupBy { get; private set; }
        public bool IsGroupBySelectPattern { get; private set; }

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
        private int selectIndex = -1;

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
        public Expression PreExecuteExpression { get; set; }
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
                    var ofTypeMethod = QueryableOfTypeMethod.MakeGenericMethodFast(typeof(TEntity));
                    root = Expression.Call(ofTypeMethod, root);
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
                        var mceMethod = mce.Method;
                        var genericArgs = mceMethod.GetGenericArguments();
                        if (genericArgs.Any(x => x.IsAssignableFrom(ItemType)))
                        {
                            MethodInfo actualMethod;
                            if (mceMethod.IsGenericMethod)
                            {
                                genericArgs = genericArgs.Select(x => x.IsAssignableFrom(ItemType) ? ItemType : x).ToArray();
                                actualMethod = mceMethod.GetGenericMethodDefinition().MakeGenericMethodFast(genericArgs);
                            }
                            else
                                actualMethod = mceMethod;
                            var source = stages[i - 1];
                            var expression = mce.Arguments.ElementAtOrDefault(1);
                            if (expression == null)
                            {
                                stages[i] = Expression.Call(actualMethod, source);

                            }
                            else
                            {
                                if (expression is UnaryExpression unary && unary?.Operand is LambdaExpression lambda && lambda.Parameters.Any(x => x.Type.IsAssignableFrom(ItemType)))
                                {
                                    expression = Expression.MakeUnary(unary.NodeType, lambda.CastParamType(this.ItemType), unary.Type);
                                }
                                stages[i] = Expression.Call(actualMethod, source, expression);
                            }
                        }
                    }
                }
            }
            else
            {
                stages = new List<Expression>() { node };
            }

            var lastStage = stages.LastOrDefault();
            if (lastStage is MethodCallExpression { Method.Name: Q.Count or Q.LongCount or Q.Any or Q.First or Q.FirstOrDefault or Q.Single or Q.SingleOrDefault } mce2)
            {
                // 将执行过滤参数转为Where过滤参数 .First(x=>true) => .Where(x=>true).First()
                var lastStageFilter = mce2.Arguments.ElementAtOrDefault(1);
                if (lastStageFilter is UnaryExpression unary && unary?.Operand is LambdaExpression lambda &&
                    lambda.Parameters.Any(x => x.Type.IsAssignableFrom(ItemType)))
                {
                    var whereMethod = QueryableWhereMethod.MakeGenericMethodFast(ItemType);
                    var mce2Argument = mce2.Arguments[0];
                    lastStageFilter = Expression.Call(whereMethod,
                        mce2Argument, lambda.CastParamType(this.ItemType));

                    mce2 = mce2.Method.Name switch
                    {
                        Q.First => Expression.Call(QueryableFirstMethod.MakeGenericMethodFast(ItemType),
                            mce2Argument),
                        Q.FirstOrDefault => Expression.Call(
                            QueryableFirstOrDefaultMethod.MakeGenericMethodFast(ItemType), mce2Argument),
                        Q.Single => Expression.Call(QueryableSingleMethod.MakeGenericMethodFast(ItemType),
                            mce2Argument),
                        Q.SingleOrDefault => Expression.Call(
                            QueryableSingleOrDefaultMethod.MakeGenericMethodFast(ItemType), mce2Argument),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    stages.RemoveAt(stages.Count - 1);
                    stages.AddRange(lastStageFilter, mce2);
                }
            }

            this.ValidateStages(stages);

            // 检测GroupBy模式
            var groupByIndex = stages.FindIndex(x => x is MethodCallExpression { Method.Name: Q.GroupBy });
            var selectIndex = stages.FindIndex(x => x is MethodCallExpression { Method.Name: Q.Select or Q.SelectMany });
            var cursorIndex = stages.FindIndex(x => x is MethodCallExpression { Method.Name: Q.Skip or Q.Take });

            this.HasGroupBy = groupByIndex != -1;
            this.IsGroupBySelectPattern = groupByIndex != -1 && selectIndex != -1 && selectIndex == groupByIndex + 1;

            if (selectIndex != -1 || cursorIndex != -1)
            {
                this.selectIndex = Math.Min(selectIndex, cursorIndex + 1);
            }

            if (lastStage is MethodCallExpression { Method.Name: Q.Count or Q.LongCount or Q.Any or Q.Sum or Q.Min or Q.MinBy or Q.Average or Q.Max or Q.MaxBy or Q.First or Q.FirstOrDefault or Q.Single or Q.SingleOrDefault } mce1)
            {
                var selectStage = stages.ElementAtOrDefault(stages.Count - 2);
                if (this.IsGroupBySelectPattern)
                {
                    // 对于GroupBy().Select().Execute()模式，将GroupBy().Select()作为整体处理
                    PreSelectExpression = selectStage;
                    PostSelectExpression = null;
                }
                else if (selectIndex == -1)
                {
                    PreSelectExpression = selectStage;
                }
                else
                {
                    PreSelectExpression = stages[selectIndex - 1];
                    if (stages.Count - 1 > selectIndex)
                    {
                        PostSelectExpression = selectStage;
                    }
                }
                PreExecuteExpression = selectStage;
                ExecuteExpression = mce1;
            }
            else
            {
                if (this.IsGroupBySelectPattern)
                {
                    // 对于GroupBy().Select()模式（无执行方法），将整个表达式作为PreSelectExpression
                    PreSelectExpression = lastStage;
                    PostSelectExpression = null;
                }
                else if (selectIndex == -1)
                {
                    PreSelectExpression = lastStage;
                }
                else
                {
                    PreSelectExpression = stages[selectIndex - 1];
                    if (stages.Count > selectIndex)
                    {
                        PostSelectExpression = stages.ElementAtOrDefault(stages.Count - 1);
                    }
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
