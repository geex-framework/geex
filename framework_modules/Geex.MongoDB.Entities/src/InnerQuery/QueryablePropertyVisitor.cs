using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Entities;

namespace Geex.MongoDB.Entities.InnerQuery
{
    public class QueryablePropertyVisitor(ParameterExpression parameter) : ExpressionVisitor
    {
        public HashSet<PropertyInfo> Properties { get; } = new HashSet<PropertyInfo>();

        protected override Expression VisitMember(MemberExpression node)
        {
            // 检查是否是直接访问参数的属性
            if (node.Expression == parameter)
            {
                if (node.Member is PropertyInfo { SetMethod.IsSpecialName: true } property)
                {
                    Properties.AddIfNotContains(property);
                }
                else
                {
                    throw new NotSupportedException($"Project fields must be pure property, incorrect field: [{node.Member.DeclaringType?.FullName ?? node.ToString()}.{node.Member.Name}]");
                }
            }

            return base.VisitMember(node);
        }
    }
}
