using HotChocolate.Types;

namespace Geex.Abstractions.Gql.Directives
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
