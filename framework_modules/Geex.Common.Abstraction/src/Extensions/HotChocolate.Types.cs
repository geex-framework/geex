using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Fasterflect;

using Geex.Common;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Gql;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.Gql.Types.Scalars;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using Geex.Common.Authorization;
using Geex.Common.Gql.Types;

using HotChocolate.Data.Filters;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Humanizer;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types
{
    public static class HotChocolateTypesExtension
    {
        public static void ConfigEntity<T>(
            this IObjectTypeDescriptor<T> @this) where T : class, IEntity
        {
            @this.IgnoreMethods();
            @this.AuthorizeFieldsImplicitly();
            if (typeof(T).IsAssignableTo<IAuditEntity>())
            {
                @this.Field(x => ((IAuditEntity)x).AuditStatus);
                @this.Field(x => ((IAuditEntity)x).Submittable);
            }

            var getters = typeof(T).GetProperties().Where(x => x.PropertyType.Name == "ResettableLazy`1" || x.PropertyType.Name == "Lazy`1");
            foreach (var getter in getters)
            {
                var field = @this.Field(getter);
                var valueType = getter.PropertyType.GenericTypeArguments[0];
                field.Resolve(x => getter.GetMethod!.Invoke(x.Parent<T>(), Array.Empty<object>())?.GetLazyValue(valueType));
                field.Type(valueType);
            }
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
                .AddInterfaceType<IAuditEntity>(x =>
                {
                    x.BindFieldsExplicitly();
                    //x.Implements<IEntityType>();
                    x.Field(y => y.AuditStatus);
                    x.Field(y => y.Submittable);
                })
                .AddInterfaceType<IPagedList>()
                .BindRuntimeType<ObjectId, ObjectIdType>()
                .BindRuntimeType<MediaType, MimeTypeType>()
                //.BindRuntimeType<byte[], Base64StringType>()
                .BindRuntimeType<JsonNode, JsonNodeType>();

            return builder;
        }


        public static string GetAggregateAuthorizePrefix<TAggregate>(this IObjectTypeDescriptor<TAggregate> @this)
        {
            var moduleName = typeof(TAggregate).DomainName();
            var entityName = typeof(TAggregate).Name.ToCamelCase();
            var prefix = $"{moduleName}_query_{entityName}";
            return prefix;
        }

        //public static string GetAggregateAuthorizePrefix<TAggregate>(this IInterfaceTypeDescriptor<TAggregate> @this)
        //{

        //    var moduleName = typeof(TAggregate).Assembly.GetName().Name.Split(".").ToList().Where(x => !x.IsIn("Gql", "Api", "Core", "Tests")).Last().ToCamelCase();
        //    var entityName = typeof(TAggregate).Name.RemovePreFix("I").ToCamelCase();
        //    var prefix = $"{moduleName}_query_{entityName}";
        //    return prefix;
        //}

        public static IObjectTypeDescriptor<T> AuthorizeWithDefaultName<T>(this IObjectTypeDescriptor<T> @this)
        {
            var trace = new StackTrace();
            //获取是哪个类来调用的
            var caller = trace.GetFrame(1).GetMethod();
            var callerDeclaringType = caller.DeclaringType;
            var moduleName = callerDeclaringType.Namespace.Split(".").ToList().Where(x => !x.IsIn("Gql", "Api", "Core", "Tests")).Last().ToCamelCase();
            var className = callerDeclaringType.Name;
            var prefix = "";
            var logger = (@this as IHasDescriptorContext)!.Context.Services.GetService<ILogger<IObjectTypeDescriptor<T>>>();
            if (className.Contains("Query"))
            {
                prefix = $"{moduleName}_query";

            }
            else if (className.Contains("Mutation"))
            {
                prefix = $"{moduleName}_mutation";

            }
            else if (className.Contains("Subscription"))
            {
                prefix = $"{moduleName}_subscription";
            }

            var propertyList = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var item in propertyList)
            {
                var policy = $"{prefix}_{item.Name.RemovePreFix("Get").ToCamelCase()}";
                if (AppPermission.List.Any(x => x.Value == policy) && AppPermission.List.Any(x => x.Value == policy))
                {
                    @this.Field(item).Authorize(policy);
                    logger.LogInformation($@"成功匹配权限规则:{policy} for {item.DeclaringType.Name}.{item.Name}");
                }
                else
                {
                    logger.LogWarning($@"跳过匹配权限规则:{item.DeclaringType.Name}.{item.Name}");
                }
            }

            // 判断是否继承了审核基类
            if (typeof(T).GetInterfaces().Contains(typeof(IHasAuditMutation)))
            {
                var auditMutationType = typeof(T).GetInterfaces().First(x => x.Name.StartsWith(nameof(IHasAuditMutation) + "`1"));
                var auditPropertyList = auditMutationType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var entityType = auditMutationType.GenericTypeArguments[0];
                foreach (var item in auditPropertyList)
                {
                    var policy = $"{prefix}_{item.Name.ToCamelCase()}{entityType.Name.RemovePreFix("I")}";

                    if (AppPermission.List.Any(x => x.Value == policy) && AppPermission.List.Any(x => x.Value == policy))
                    {
                        // gql版本限制, 重写resolve的字段需要重新指定类型
                        @this.Field(policy.Split('_').Last()).Type<BooleanType>().Authorize(policy);
                        logger.LogInformation($@"成功匹配权限规则:{policy} for {typeof(T).Name}.{item.Name}");
                    }
                    else
                    {
                        logger.LogWarning($@"跳过匹配权限规则:{typeof(T).Name}.{item.Name}");
                    }
                }
            }
            return @this;
        }

        private static MethodInfo GetFieldsMethodInfo = typeof(ObjectTypeDescriptor).GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod;
        public static ICollection<ObjectFieldDescriptor> GetFields<T>(this IObjectTypeDescriptor<T> descriptor)
        {
            return GetFieldsMethodInfo.Invoke(descriptor, new object?[] { }) as ICollection<ObjectFieldDescriptor>;
        }
        public static void AuthorizeFieldsImplicitly<T>(this IObjectTypeDescriptor<T> descriptor) where T : class
        {
            var propertyList = descriptor.GetFields();
            foreach (var item in propertyList)
            {
                descriptor.FieldWithDefaultAuthorize(item);
            }
        }

        public static IObjectFieldDescriptor FieldWithDefaultAuthorize<T>(this IObjectTypeDescriptor<T> @this, IObjectFieldDescriptor fieldDescriptor)
        {
            var propertyOrMethod = ((ObjectFieldDefinition)fieldDescriptor.GetPropertyValue("Definition")).Member.DeclaringType;
            var prefix = @this.GetAggregateAuthorizePrefix();
            var logger = (@this as IHasDescriptorContext)!.Context.Services.GetService<ILogger<IObjectTypeDescriptor<T>>>();
            var policy = $"{prefix}_{propertyOrMethod.Name.ToCamelCase()}";
            if (AppPermission.List.Any(x => x.Value == policy) && AppPermission.List.Any(x => x.Value == policy))
            {
                fieldDescriptor = fieldDescriptor.Authorize(policy);
                logger.LogInformation($@"成功匹配权限规则:{policy} for {propertyOrMethod.DeclaringType?.Name}.{propertyOrMethod.Name}");
            }
            else
            {
                logger.LogDebug($@"跳过匹配权限规则:{propertyOrMethod.DeclaringType?.Name}.{propertyOrMethod.Name}");
            }

            return fieldDescriptor;
        }


        public static IObjectFieldDescriptor FieldWithDefaultAuthorize<T, TValue>(this IObjectTypeDescriptor<T> @this, Expression<Func<T, TValue>> propertyOrMethod)
        {
            if (propertyOrMethod.Body.NodeType == ExpressionType.Call)
            {
                return @this.FieldWithDefaultAuthorize((propertyOrMethod.Body as MethodCallExpression)!.Method);
            }
            return @this.FieldWithDefaultAuthorize((propertyOrMethod.Body as MemberExpression)!.Member);
        }

        public static IObjectFieldDescriptor FieldWithDefaultAuthorize<T>(this IObjectTypeDescriptor<T> @this, MemberInfo propertyOrMethod)
        {
            var prefix = @this.GetAggregateAuthorizePrefix();
            var fieldDescriptor = @this.Field(propertyOrMethod);
            var logger = (@this as IHasDescriptorContext)!.Context.Services.GetService<ILogger<IObjectTypeDescriptor<T>>>();
            var policy = $"{prefix}_{propertyOrMethod.Name.ToCamelCase()}";
            if (AppPermission.List.Any(x => x.Value == policy) && AppPermission.List.Any(x => x.Value == policy))
            {
                fieldDescriptor = fieldDescriptor.Authorize(policy);
                logger.LogInformation($@"成功匹配权限规则:{policy} for {propertyOrMethod.DeclaringType?.Name}.{propertyOrMethod.Name}");
            }
            else
            {
                logger.LogDebug($@"跳过匹配权限规则:{propertyOrMethod.DeclaringType?.Name}.{propertyOrMethod.Name}");
            }

            return fieldDescriptor;
        }

        internal static void ConfigExtensionFields<T>(this IObjectTypeDescriptor<T> descriptor)
        {
            var type = typeof(T);
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Kind))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Scope))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Name))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.Description))).Ignore();
            descriptor.Field(type.GetProperty(nameof(ObjectTypeExtension.ContextData))).Ignore();
        }
        public static IObjectTypeDescriptor<T> Ignore<T>(
      this IObjectTypeDescriptor<T> descriptor)
        {
            GeexTypeInterceptor.IgnoredTypes.AddIfNotContains(typeof(T));
            return descriptor;
        }

        public static IInputObjectTypeDescriptor<T> IsOneOf<T, TParent>(
     this IInputObjectTypeDescriptor<T> descriptor) where T : TParent
        {
            GeexTypeInterceptor.OneOfConfigs.AddIfNotContains(new KeyValuePair<Type, Type>(typeof(TParent), typeof(T)));
            return descriptor;
        }
    }
}
