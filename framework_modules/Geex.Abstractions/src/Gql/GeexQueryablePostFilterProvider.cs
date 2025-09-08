using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace Geex.Gql
{
    public class GeexQueryablePostFilterProvider : QueryableFilterProvider
    {
        public GeexQueryablePostFilterProvider(
      Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
      : base(configure)
        {
        }

        public static Dictionary<int, PropertyInfo> PostFilterFields { get; set; } = new Dictionary<int, PropertyInfo>();


        public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
        {
            return (FieldMiddleware)(next => (FieldDelegate)(context => ExecuteAsync(next, context)));

            async ValueTask ExecuteAsync(FieldDelegate next, IMiddlewareContext context)
            {
                await next(context).ConfigureAwait(false);
                IInputField inputField = context.Selection.Field.Arguments[(string)argumentName];
                IValueNode filterValueNode = !context.LocalContextData.ContainsKey(QueryableFilterProvider.ContextValueNodeKey) || !(context.LocalContextData[QueryableFilterProvider.ContextValueNodeKey] is IValueNode valueNode) ? context.ArgumentLiteral<IValueNode>(argumentName) : valueNode;
                object obj1;
                bool flag1 = context.LocalContextData.TryGetValue(QueryableFilterProvider.SkipFilteringKey, out obj1) && obj1 is bool flag && flag;
                object obj2;
                if (filterValueNode.IsNull() | flag1 || !(inputField.Type is IFilterInputType type) || !context.Selection.Field.ContextData.TryGetValue(QueryableFilterProvider.ContextVisitFilterArgumentKey, out obj2) || !(obj2 is VisitFilterArgument visitFilterArgument))
                    return;
                bool inMemory = context.Result is QueryableExecutable<TEntityType> result && result.InMemory || !(context.Result is IQueryable) || context.Result is EnumerableQuery;
                QueryableFilterContext context1 = visitFilterArgument(filterValueNode, type, inMemory);
                //if ((context1.Scopes.FirstOrDefault()?.Level.FirstOrDefault()?.FirstOrDefault() is BinaryExpression binaryExpression) && binaryExpression.Left is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo && !propertyInfo.CanWrite)
                //{

                //}
                Expression<Func<TEntityType, bool>> expression;
                if (context1.TryCreateLambda<Func<TEntityType, bool>>(out expression))
                {
                    IMiddlewareContext middlewareContext = context;
                    object obj3;
                    switch (context.Result)
                    {
                        case IQueryable<TEntityType> source1:
                            var data = source1.WhereWithPostFilter(expression);
                            obj3 = data;
                            break;
                        case IEnumerable<TEntityType> source2:
                            obj3 = (object)source2.AsQueryable<TEntityType>().Where<TEntityType>(expression);
                            break;
                        case QueryableExecutable<TEntityType> queryableExecutable:
                            obj3 = (object)queryableExecutable.WithSource(queryableExecutable.Source.Where<TEntityType>(expression));
                            break;
                        default:
                            obj3 = context.Result;
                            break;
                    }
                    middlewareContext.Result = obj3;
                }
                else
                {
                    if (context1.Errors.Count <= 0)
                        return;
                    context.Result = (object)Array.Empty<TEntityType>();
                    foreach (IError error in (IEnumerable<IError>)context1.Errors)
                        context.ReportError(error.WithPath(context.Path));
                }


            }
        }
    }
}
