using System;
using System.Linq;
using System.Threading.Tasks;

using HotChocolate.Resolvers;

using MongoDB.Entities;

namespace Geex.Gql.AutoBatchLoad
{
    public class AutoBatchLoadMiddleware
    {
        public async Task InvokeAsync(IMiddlewareContext context, FieldDelegate next)
        {
            await next(context).ConfigureAwait(false);

            if (!context.Selection.Field.AutoBatchLoadEnabled)
            {
                return;
            }

            switch (context.Result)
            {
                case IQueryable queryable when TryGetEntityElementType(queryable, out var elementType):
                    context.MergeQueryableBatchLoad(queryable, elementType);
                    break;
                case IEntityBase entity:
                    context.LoadEntityBatchLoad(entity);
                    break;
            }
        }

        private static bool TryGetEntityElementType(IQueryable queryable, out Type elementType)
        {
            elementType = queryable.ElementType;
            return typeof(IEntityBase).IsAssignableFrom(elementType);
        }
    }
}
