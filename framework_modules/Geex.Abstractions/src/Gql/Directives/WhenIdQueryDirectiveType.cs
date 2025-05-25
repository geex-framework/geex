//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using HotChocolate.Configuration;
//using HotChocolate.Data.Filters;
//using HotChocolate.Language;
//using HotChocolate.Types;
//using HotChocolate.Types.Descriptors.Definitions;

//using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

//namespace Geex.Abstractions.Gql.Directives
//{
//    public class WhenIdQueryDirectiveType : DirectiveType
//    {
//        protected override void Configure(IDirectiveTypeDescriptor descriptor)
//        {
//            descriptor.Name("whenIdQuery");
//            descriptor.Location(DirectiveLocation.Field);
//            descriptor.Use((next) => (context) =>
//            {
//                var filter = context.Operation.VariableDefinitions.FirstOrDefault(x => x.Type.NamedType().Name.Value.EndsWith("FilterInput"));
//                if (filter != default)
//                {
//                    var value = context.Variables.FirstOrDefault(x=>x.Name == filter.Variable.Value).Value?.Value as List<ObjectFieldNode>;
//                    if (value != default)
//                    {
//                        var idField = value.FirstOrDefault(x=>x.Name.Value == "id");
//                        if (idField == default)
//                        {
//                            context.Result = null;
//                        }
//                    }
//                }
//                return next(context);
//            });
//            base.Configure(descriptor);
//        }
//    }
//}
