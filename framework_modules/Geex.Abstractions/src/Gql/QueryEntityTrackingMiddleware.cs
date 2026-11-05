using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Gql;

public sealed class QueryEntityTrackingMiddleware(FieldDelegate next)
{
    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        if (context.Operation.Type == OperationType.Query)
        {
            var work = context.Services.GetService<IUnitOfWork>();
            if (work != null)
            {
                work.DbContext.EntityTrackingEnabled = false;
            }
        }

        return next(context);
    }
}
