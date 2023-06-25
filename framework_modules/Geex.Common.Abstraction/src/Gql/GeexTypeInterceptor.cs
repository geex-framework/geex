using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.Common.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexTypeInterceptor : TypeInterceptor
    {
        static MethodInfo AddObjectTypeMethod = typeof(SchemaBuilderExtensions).GetMethods().First(x => x is { Name: nameof(SchemaBuilderExtensions.AddObjectType), ContainsGenericParameters: true } && x.GetParameters().Length > 1);
        public static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();

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

            var gqlConfigs = context.Services.GetServices<IEntityGqlConfig>();

            foreach (var gqlConfig in gqlConfigs)
            {
                var gqlConfigType = gqlConfig.GetType();
                var entityType = gqlConfigType.BaseType.GetGenericArguments().First();
                var configureMethodInfo = gqlConfigType.GetMethod(nameof(IEntityGqlConfig.Configure), BindingFlags.NonPublic | BindingFlags.Instance);
                var descriptorType = configureMethodInfo.GetParameters().First().ParameterType;
                var parameter = Expression.Parameter(descriptorType, "descriptor");
                var callExpression = Expression.Call(Expression.Constant(gqlConfig), configureMethodInfo, parameter);
                var lambda = Expression.Lambda(callExpression, parameter).Compile();
                AddObjectTypeMethod.MakeGenericMethod(entityType).Invoke(null, new object?[] { schemaBuilder, lambda });
            }
        }

        /// <inheritdoc />
        public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
        {
            base.OnCreateSchemaError(context, error);
        }
    }
}
