using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;

namespace MongoDB.Entities.Utilities
{
    public static class ExpressionExtensions
    {
        internal static Expression ReplaceSource(this Expression originExpression, object source, ReplaceType replaceType)
        {
            if (originExpression is MethodCallExpression mce)
            {
                var expression = new ReplaceSourceValueVisitor(source, replaceType).Visit(mce);
                return expression;
            }
            return Expression.Constant(source);
        }

        public static LambdaExpression CastParamType<T>(this LambdaExpression originExpression)
        {
            if (originExpression.Parameters.First().Type == typeof(T))
            {
                return originExpression;
            }
            //parameter that will be used in generated expression
            var param = Expression.Parameter(typeof(T), originExpression.Parameters.First().Name);
            //visiting body of original expression that gives us body of the new expression
            var body = new CastParamTypeVisitor<T>(param).Visit((originExpression.Body));
            //generating lambda expression form body and parameter
            //notice that this is what you need to invoke the Method_2
            LambdaExpression lambda = Expression.Lambda(body, param);
            return lambda;
        }

        public static LambdaExpression CastParamType(this LambdaExpression originExpression, Type targetType)
        {
            if (originExpression.Parameters.First().Type == targetType)
            {
                return originExpression;
            }
            //parameter that will be used in generated expression
            var param = Expression.Parameter(targetType, originExpression.Parameters.First().Name);
            //visiting body of original expression that gives us body of the new expression
            ;
            var body =
                (Activator.CreateInstance(typeof(CastParamTypeVisitor<>).MakeGenericType(targetType), args: param) as
                    ExpressionVisitor).Visit((originExpression.Body));
            //generating lambda expression form body and parameter
            //notice that this is what you need to invoke the Method_2
            LambdaExpression lambda = Expression.Lambda(body, param);
            return lambda;
        }
    }

    internal class CastParamTypeVisitor<T> : ExpressionVisitor
    {
        ParameterExpression _parameter;

        //there must be only one instance of parameter expression for each parameter
        //there is one so one passed here
        public CastParamTypeVisitor(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        //this method replaces original parameter with given in constructor
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // 类型可以转换才进行转换
            if (node.Type.IsAssignableFrom(typeof(T)) || typeof(T).IsAssignableFrom(node.Type))
            {
                return _parameter;
            }
            // 可能为嵌套参数查询, 忽略
            return base.VisitParameter(node);
        }

        //this one is required because PersonData does not implement IPerson and it finds
        //property in PersonData with the same name as the one referenced in expression
        //and declared on IPerson
        protected override Expression VisitMember(MemberExpression node)
        {
            //only properties are allowed if you use fields then you need to extend
            // this method to handle them
            if (node.Member.MemberType != System.Reflection.MemberTypes.Property || node.Expression?.Type == null)
                return node;
            // 类型不匹配直接跳过
            if (!node.Expression.Type.IsAssignableFrom(typeof(T)))
            {
                if (node.Expression.NodeType == ExpressionType.Parameter)
                {
                    return node;
                }
            }
            //name of a member referenced in original expression in your
            //sample Id in mine Prop
            var memberName = node.Member.Name;
            //find property on type T (=PersonData) by name
            var otherMember = node.Expression.Type.GetProperty(memberName);
            var inner = Visit(node.Expression);
            // 接口可能未直接定义id字段, 向上递归寻找
            otherMember ??= inner.Type.GetProperty(memberName) ?? node.Member as PropertyInfo;
            //visit left side of this expression p.Id this would be p

            //return Expression.Property(node.Expression, otherMember);
            return Expression.Property(inner, otherMember);
        }
    }

    internal class StringToObjectIdVisitor : ExpressionVisitor
    {

        //there must be only one instance of parameter expression for each parameter
        //there is one so one passed here
        public StringToObjectIdVisitor()
        {
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            object convertedValue;
            if (node.Value is IEnumerable<string> ids)
            {
                convertedValue = ids.Select(id =>
                       (object)(id is string str && ObjectId.TryParse(str, out var objectId) ? objectId : id));
            }
            else
            {
                convertedValue = node.Value is string str && ObjectId.TryParse(str, out var objectId) ? objectId : node.Value;
            }
            return Expression.Constant(convertedValue);
        }
    }

    internal class FindMemberAccessVisitor<T> : ExpressionVisitor
    {
        public bool IsStringAsObjectId { get; set; }
        //there must be only one instance of parameter expression for each parameter
        //there is one so one passed here
        public FindMemberAccessVisitor()
        {

        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.GetCustomAttribute<ObjectIdAttribute>() != default)
            {
                IsStringAsObjectId = true;
            }
            return base.VisitMember(node);
        }
    }

    internal class ReplaceSourceValueVisitor : ExpressionVisitor
    {
        object _newValue;
        private readonly ReplaceType _replaceType;

        public ReplaceSourceValueVisitor(object newValue, ReplaceType replaceType)
        {
            _newValue = newValue;
            this._replaceType = replaceType;
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (_replaceType)
            {
                case ReplaceType.OriginSource:
                    if (node.Method.DeclaringType == typeof(Queryable) && node.Arguments.FirstOrDefault() is ConstantExpression)
                    {
                        return node.Update(null, new[] { Expression.Constant(_newValue) }.Concat(node.Arguments.Skip(1)));
                    }
                    break;
                case ReplaceType.SelectSource:
                    if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name is nameof(Queryable.Select) or nameof(Queryable.SelectMany) || node.Arguments.ElementAtOrDefault(0) is ConstantExpression)
                    {
                        return node.Update(null, new[] { Expression.Constant(_newValue) }.Concat(node.Arguments.Skip(1)));
                    }
                    break;
                case ReplaceType.DirectSource:
                    return node.Update(null, new[] { Expression.Constant(_newValue) }.Concat(node.Arguments.Skip(1)));
            }

            return base.VisitMethodCall(node);
        }
    }

    public enum ReplaceType
    {
        /// <summary>
        /// origin.Where().Select().Distinct() => new.Where().Select().Distinct()
        /// </summary>
        OriginSource,
        /// <summary>
        /// origin.Where().Select().Distinct() => new.Select().Distinct()
        /// </summary>
        SelectSource,
        /// <summary>
        /// origin.Where().Select().Distinct() => new.Distinct()
        /// </summary>
        DirectSource
    }
}
