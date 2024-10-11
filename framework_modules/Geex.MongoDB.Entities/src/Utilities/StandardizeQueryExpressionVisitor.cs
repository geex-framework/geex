using System;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Entities.Utilities;

public class StandardizeQueryExpressionVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable))
        {
            var source = Visit(node.Arguments[0]);
            var predicate = node.Arguments.Count > 1 ? Visit(node.Arguments[1]) : null;

            switch (node.Method.Name)
            {
                case "Count":
                case "LongCount":
                    if (predicate != null)
                    {
                        var whereCall = Expression.Call(
                            typeof(Queryable),
                            "Where",
                            node.Method.GetGenericArguments(),
                            source,
                            predicate
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            whereCall
                        );
                    }
                    break;

                case "Any":
                    if (predicate != null)
                    {
                        var whereCall = Expression.Call(
                            typeof(Queryable),
                            "Where",
                            node.Method.GetGenericArguments(),
                            source,
                            predicate
                        );
                        var takeCall = Expression.Call(
                            typeof(Queryable),
                            "Take",
                            node.Method.GetGenericArguments(),
                            whereCall,
                            Expression.Constant(1)
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            takeCall
                        );
                    }
                    break;
                case "First":
                case "FirstOrDefault":
                    if (predicate != null)
                    {
                        var whereCall = Expression.Call(
                            typeof(Queryable),
                            "Where",
                            node.Method.GetGenericArguments(),
                            source,
                            predicate
                        );
                        var takeCall = Expression.Call(
                            typeof(Queryable),
                            "Take",
                            node.Method.GetGenericArguments(),
                            whereCall,
                            Expression.Constant(1)
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            takeCall
                        );
                    }
                    else
                    {
                        var takeCall = Expression.Call(
                            typeof(Queryable),
                            "Take",
                            node.Method.GetGenericArguments(),
                            source,
                            Expression.Constant(1)
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            takeCall
                        );
                    }
                    break;

                case "Single":
                case "SingleOrDefault":
                    if (predicate != null)
                    {
                        var whereCall = Expression.Call(
                            typeof(Queryable),
                            "Where",
                            node.Method.GetGenericArguments(),
                            source,
                            predicate
                        );
                        var takeCall = Expression.Call(
                            typeof(Queryable),
                            "Take",
                            node.Method.GetGenericArguments(),
                            whereCall,
                            Expression.Constant(2)
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            takeCall
                        );
                    }
                    else
                    {
                        var takeCall = Expression.Call(
                            typeof(Queryable),
                            "Take",
                            node.Method.GetGenericArguments(),
                            source,
                            Expression.Constant(2)
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            node.Method.GetGenericArguments(),
                            takeCall
                        );
                    }
                    break;

                case "Sum":
                case "Min":
                case "Max":
                case "Average":
                    if (predicate != null)
                    {
                        var funcArgs = predicate.Type.GetGenericArguments()[0];
                        var selectArgs = funcArgs.GetGenericArguments();
                        var entityType = selectArgs[0];
                        var resultType = selectArgs[1];
                        var selectCall = Expression.Call(
                            typeof(Queryable),
                            "Select",
                            new Type[] { entityType, resultType },
                            source,
                            predicate
                        );
                        return Expression.Call(
                            typeof(Queryable),
                            node.Method.Name,
                            new Type[] { },
                            selectCall
                        );
                    }
                    break;
            }
        }
        return base.VisitMethodCall(node);
    }
}