using System.Linq.Expressions;
using System.Reflection;

using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace Geex.Common
{
    public class ComplexFilterFieldHandler : FilterOperationHandler<QueryableFilterContext, Expression>
    {
        public override bool CanHandle(ITypeCompletionContext context, IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return (fieldDefinition.Member is PropertyInfo { CanWrite: false });
        }

        public override bool TryHandleOperation(QueryableFilterContext context, IFilterOperationField field, ObjectFieldNode node,
            out Expression result)
        {
            return base.TryHandleOperation(context, field, node, out result);
        }

        public override bool TryHandleEnter(QueryableFilterContext context, IFilterField field, ObjectFieldNode node,
            out ISyntaxVisitorAction? action)
        {
            return base.TryHandleEnter(context, field, node, out action);
        }

        public override bool TryHandleLeave(QueryableFilterContext context, IFilterField field, ObjectFieldNode node,
            out ISyntaxVisitorAction? action)
        {
            return base.TryHandleLeave(context, field, node, out action);
        }
    }
}