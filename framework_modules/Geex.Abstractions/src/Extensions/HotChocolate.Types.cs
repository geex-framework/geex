using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

using Fasterflect;
using Geex;
using Geex.Common;
using Geex.Abstractions;
using Geex.Gql;
using Geex.Gql.Types;
using Geex.Gql.Types.Scalars;
using Geex.Storage;
using HotChocolate.Data.Filters;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Entities;

using ExpressionType = System.Linq.Expressions.ExpressionType;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types
{
    public static class HotChocolateTypesExtension
    {
        [Obsolete]
        public static void ConfigEntity<T>(
            this IObjectTypeDescriptor<T> @this) where T : class, IEntity
        {
            //@this.IgnoreMethods();
        }

        public static IInterfaceTypeDescriptor<T> IgnoreMethods<T>(
      this IInterfaceTypeDescriptor<T> descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            // specialname过滤属性
            foreach (var method in typeof(T).GetMethods().Where(x => !x.IsSpecialName))
            {
                if (method.ReturnType != typeof(void))
                {
                    var exp = Expression.Lambda(Expression.Convert(Expression.Call(Expression.Parameter(typeof(T), "x"), method, method.GetParameters().Select(x => Expression.Default(x.ParameterType))), typeof(object)), Expression.Parameter(typeof(T), "x"));
                    descriptor.Field(exp as Expression<Func<T, object>>).Ignore();
                }
                //else
                //{
                //    var exp = Expression.Lambda(Expression.Call(Expression.Parameter(typeof(T), "x"), method, method.GetParameters().Select(x => Expression.Default(x.ParameterType))), Expression.Parameter(typeof(T), "x"));
                //    descriptor.Field(exp as Expression<Func<T, object>>).Ignore();
                //}
                //descriptor.Field(method.Name).Ignore();
            }
            return descriptor;
        }

        public static IObjectTypeDescriptor<T> IgnoreMethods<T>(
      this IObjectTypeDescriptor<T> descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            // specialname过滤属性
            foreach (var method in typeof(T).GetMethods().Where(x => !x.IsSpecialName))
            {
                descriptor.Field(method).Ignore();
            }
            return descriptor;
        }

        public static IFilterFieldDescriptor PostFilterField<T, TField>(
            this IFilterInputTypeDescriptor<T> @this,
            Expression<Func<T, TField>> property)
        {
            // <TField>(Expression<Func<T, TField>>
            var field = @this.Field<TField>(property);
            var prop = ((property.Body as MemberExpression).Member as PropertyInfo);
            GeexQueryablePostFilterProvider.PostFilterFields.Add(prop.GetHashCode(), prop);
            return field;
        }

        public static IRequestExecutorBuilder AddCommonTypes(
      this IRequestExecutorBuilder builder)
        {
            builder
                .AddInterfaceType<IEntityBase>(x =>
                {
                    x.BindFieldsExplicitly();
                    x.Field(y => y.Id);
                    x.Field(y => y.CreatedOn);
                })
                .AddInterfaceType<IPagedList>()
                .BindRuntimeType<ObjectId, ObjectIdType>()
                .BindRuntimeType<MediaType, MimeTypeType>()
                //.BindRuntimeType<byte[], Base64StringType>()
                .BindRuntimeType<JsonNode, JsonNodeType>();

            return builder;
        }        //public static string GetAggregateAuthorizePrefix<TAggregate>(this IInterfaceTypeDescriptor<TAggregate> @this)
        //{
        //    var moduleName = typeof(TAggregate).Assembly.GetName().Name.Split(".").ToList().Where(x => !x.IsIn("Gql", "Api", "Core", "Tests")).Last().ToCamelCase();
        //    var entityName = typeof(TAggregate).Name.RemovePreFix("I").ToCamelCase();
        //    var prefix = $"{moduleName}_query_{entityName}";
        //    return prefix;
        //}

        private static MethodInfo GetFieldsMethodInfo = typeof(ObjectTypeDescriptor).GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod;
        public static ICollection<ObjectFieldDescriptor> GetFields<T>(this IObjectTypeDescriptor<T> descriptor)
        {
            return GetFieldsMethodInfo.Invoke(descriptor, new object?[] { }) as ICollection<ObjectFieldDescriptor>;
        }

        /// <summary>
        /// Ignore extension fields for object type descriptors
        /// </summary>
        internal static void IgnoreExtensionFields<T>(this IObjectTypeDescriptor<T> descriptor)
        {
            var type = typeof(T);
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Kind))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Scope))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Name))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Description))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.ContextData))).Ignore();
        }

        /// <summary>
        /// Mark a type to be ignored by the schema
        /// </summary>
        public static IObjectTypeDescriptor<T> Ignore<T>(
      this IObjectTypeDescriptor<T> descriptor)
        {
            GeexTypeInterceptor.IgnoredTypes.AddIfNotContains(typeof(T));
            return descriptor;
        }

        /// <summary>
        /// Mark an input object type as part of a OneOf group
        /// </summary>
        public static IInputObjectTypeDescriptor<T> IsOneOf<T, TParent>(
     this IInputObjectTypeDescriptor<T> descriptor) where T : TParent
        {
            GeexTypeInterceptor.OneOfConfigs.AddIfNotContains(new KeyValuePair<Type, Type>(typeof(TParent), typeof(T)));
            return descriptor;
        }
    }
}
