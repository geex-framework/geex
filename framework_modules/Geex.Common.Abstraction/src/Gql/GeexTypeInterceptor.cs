using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.Common.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexTypeInterceptor : TypeInterceptor
    {
        /// <inheritdoc />
        public override void OnBeforeCreateSchema(IDescriptorContext context, ISchemaBuilder schemaBuilder)
        {
            var classEnumTypes = GeexModule.ClassEnumTypes;

            foreach (var classEnumType in classEnumTypes)
            {
                if (classEnumType.GetClassEnumRealType().BaseType.GetProperty(nameof(Enumeration.DynamicValues)).GetValue(null).As<IEnumerable<IEnumeration>>().Any())
                {
                    schemaBuilder.AddConvention(typeof(IFilterConvention), sp => new FilterConventionExtension(x =>
                    {
                        x.BindRuntimeType(classEnumType, typeof(ClassEnumOperationFilterInput<>).MakeGenericType(classEnumType));
                    }));
                    schemaBuilder.BindRuntimeType(classEnumType, typeof(EnumerationType<>).MakeGenericType(classEnumType));
                }
            }
        }
    }
}
