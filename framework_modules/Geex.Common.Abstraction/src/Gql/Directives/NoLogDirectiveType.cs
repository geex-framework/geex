using System.Collections;
using System.Linq;
using System.Security.Claims;

using HotChocolate.Types;

using MongoDB.Entities;

namespace Geex.Common.Abstraction.Gql.Directives
{
    public class NoLogDirectiveType
    {

        public class Config : GqlConfig.Directive<NoLogDirectiveType>
        {
            /// <inheritdoc />
            protected override void Configure(IDirectiveTypeDescriptor<NoLogDirectiveType> descriptor)
            {
                descriptor.Name("noLog");
                descriptor.Location(DirectiveLocation.FieldDefinition);
                base.Configure(descriptor);
            }
        }
    }
}