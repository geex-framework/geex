using System.Collections;
using System.Linq;

using HotChocolate.Types;

namespace Geex.Abstractions.Gql.Directives
{
    public class FirstDirectiveType : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("first");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Argument("count").Type<IntType>().DefaultValue(1);
            descriptor.Use((next, directive) => context =>
            {
                var result = next.Invoke(context);
                result.AsTask().Wait();
                var collection = (context.Result as IEnumerable);
                context.Result = collection?.Cast<object>().Take(1);
                return result;
            });
        }
    }
}
