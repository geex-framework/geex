using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace Geex.Gql
{
    public class QueryableExecuteFilterHandler : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        public override bool CanHandle(
          ITypeCompletionContext context,
          IFilterInputTypeDefinition typeDefinition,
          IFilterFieldDefinition fieldDefinition)
        {
            return !(fieldDefinition is FilterOperationFieldDefinition);
        }


        public override bool TryHandleEnter(
          QueryableFilterContext context,
          IFilterField field,
          ObjectFieldNode node,
          [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError<Expression>(CreateNonNullError<Expression>(field, node.Value, (IFilterVisitorContext<Expression>)context));
                action = SyntaxVisitor<ISyntaxVisitorContext>.Skip;
                return true;
            }
            if (field.RuntimeType == null)
            {
                action = (ISyntaxVisitorAction)null;
                return false;
            }
            PropertyInfo member = field.Member as PropertyInfo;
            Expression nextExpression;
            if ((object)member != null)
            {
                nextExpression = (Expression)Expression.Property(context.GetInstance<Expression>(), member);
            }
            else
                nextExpression = (Expression)Expression.Call(context.GetInstance<Expression>(), field.Member as MethodInfo ?? throw new InvalidOperationException());
            context.PushInstance<Expression>(nextExpression);
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor<ISyntaxVisitorContext>.Continue;
            return true;
        }

        public override bool TryHandleLeave(
          QueryableFilterContext context,
          IFilterField field,
          ObjectFieldNode node,
          [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field.RuntimeType == null)
            {
                action = (ISyntaxVisitorAction)null;
                return false;
            }
            Expression condition = context.GetLevel<Expression>().Dequeue();
            context.PopInstance<Expression>();
            context.RuntimeTypes.Pop();
            if (context.InMemory)
                condition = FilterExpressionBuilder.NotNullAndAlso(context.GetInstance<Expression>(), condition);
            context.GetLevel<Expression>().Enqueue(condition);
            action = SyntaxVisitor<ISyntaxVisitorContext>.Continue;
            return true;
        }

        public static IError CreateNonNullError<T>(
          IFilterField field,
          IValueNode value,
          IFilterVisitorContext<T> context)
        {
            IFilterInputType type = context.Types.OfType<IFilterInputType>().First<IFilterInputType>();
            return ErrorBuilder.New().SetMessage("CreateNonNullError", context.Operations.Peek().Name, type.Print()).AddLocation((ISyntaxNode)value).SetCode("HC0026").SetExtension("expectedType", new NonNullType((IType)field.Type).Print()).SetExtension("filterType", type.Print()).Build();
        }
    }
}