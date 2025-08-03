using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// 强类型的对象字段描述符接口
    /// </summary>
    /// <typeparam name="T">父类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    public interface IObjectFieldDescriptor<T, TValue> : IObjectFieldDescriptor
    {

    }

    /// <summary>
    /// 强类型的对象字段描述符包装类
    /// </summary>
    /// <typeparam name="T">父类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    public class ObjectFieldDescriptor<T, TValue> : IObjectFieldDescriptor<T, TValue>
    {
        private readonly IObjectFieldDescriptor _descriptor;

        public ObjectFieldDescriptor(IObjectFieldDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        // 隐式转换到基础描述符
        public static implicit operator ObjectFieldDescriptor(ObjectFieldDescriptor<T, TValue> typed)
        {
            return typed._descriptor as ObjectFieldDescriptor;
        }

        // 隐式转换从基础描述符
        public static implicit operator ObjectFieldDescriptor<T, TValue>(ObjectFieldDescriptor notTyped)
        {
            return new ObjectFieldDescriptor<T, TValue>(notTyped);
        }

        /// <inheritdoc />
        IDescriptorExtension<ObjectFieldDefinition> IDescriptor<ObjectFieldDefinition>.Extend() => _descriptor.Extend();

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.SyntaxNode(FieldDefinitionNode? fieldDefinition) => _descriptor.SyntaxNode(fieldDefinition);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Name(string value) => _descriptor.Name(value);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Description(string? value) => _descriptor.Description(value);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.DeprecationReason(string? reason) => _descriptor.DeprecationReason(reason);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Deprecated(string? reason) => _descriptor.Deprecated(reason);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Deprecated() => _descriptor.Deprecated();

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Type<TOutputType>() => _descriptor.Type<TOutputType>();

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Type<TOutputType>(TOutputType outputType) => _descriptor.Type(outputType);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Type(ITypeNode typeNode) => _descriptor.Type(typeNode);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Type(Type type) => _descriptor.Type(type);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.StreamResult(bool hasStreamResult) => _descriptor.StreamResult(hasStreamResult);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Argument(string argumentName, Action<IArgumentDescriptor> argumentDescriptor) => _descriptor.Argument(argumentName, argumentDescriptor);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Ignore(bool ignore) => _descriptor.Ignore(ignore);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver) => _descriptor.Resolver(fieldResolver);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver, Type resultType) => _descriptor.Resolver(fieldResolver, resultType);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Resolve(FieldResolverDelegate fieldResolver) => _descriptor.Resolve(fieldResolver);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Resolve(FieldResolverDelegate fieldResolver, Type? resultType) => _descriptor.Resolve(fieldResolver, resultType);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.ResolveWith<TResolver>(Expression<Func<TResolver, object?>> propertyOrMethod) => _descriptor.ResolveWith(propertyOrMethod);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.ResolveWith(MemberInfo propertyOrMethod) => _descriptor.ResolveWith(propertyOrMethod);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Subscribe(SubscribeResolverDelegate subscribeResolver) => _descriptor.Subscribe(subscribeResolver);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Use(FieldMiddleware middleware) => _descriptor.Use(middleware);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Directive<T1>(T1 directiveInstance) => _descriptor.Directive(directiveInstance);

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Directive<T1>() => _descriptor.Directive<T1>();

        /// <inheritdoc />
        IObjectFieldDescriptor IObjectFieldDescriptor.Directive(string name, params ArgumentNode[] arguments) => _descriptor.Directive(name, arguments);
    }
}
