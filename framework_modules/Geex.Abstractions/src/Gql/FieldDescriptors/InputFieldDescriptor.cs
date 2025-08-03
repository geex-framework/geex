using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors
{
    public interface IInputFieldDescriptor<T, TValue> : IInputFieldDescriptor
    {

    }

    // 包装类
    public class InputFieldDescriptor<T, TValue> : IInputFieldDescriptor<T, TValue>
    {
        private readonly IInputFieldDescriptor _descriptor;

        public InputFieldDescriptor(IInputFieldDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        // 隐式转换
        public static implicit operator InputFieldDescriptor(InputFieldDescriptor<T, TValue> typed)
        {
            return typed._descriptor as InputFieldDescriptor;
        }

        /// <inheritdoc />
        public IDescriptorExtension<InputFieldDefinition> Extend()
        {
            return _descriptor.Extend();
        }

        /// <inheritdoc />
        public IInputFieldDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinition)
        {
            return _descriptor.SyntaxNode(inputValueDefinition);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Name(string value)
        {
            return _descriptor.Name(value);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Deprecated(string reason)
        {
            return _descriptor.Deprecated(reason);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Deprecated()
        {
            return _descriptor.Deprecated();
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Description(string value)
        {
            return _descriptor.Description(value);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Type<TInputType>() where TInputType : IInputType
        {
            return _descriptor.Type<TInputType>();
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Type<TInputType>(TInputType inputType) where TInputType : class, IInputType
        {
            return _descriptor.Type(inputType);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Type(ITypeNode typeNode)
        {
            return _descriptor.Type(typeNode);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Type(Type type)
        {
            return _descriptor.Type(type);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Ignore(bool ignore = true)
        {
            return _descriptor.Ignore(ignore);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor DefaultValue(IValueNode value)
        {
            return _descriptor.DefaultValue(value);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor DefaultValue(object value)
        {
            return _descriptor.DefaultValue(value);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Directive<T1>(T1 directiveInstance) where T1 : class
        {
            return _descriptor.Directive(directiveInstance);
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Directive<T1>() where T1 : class, new()
        {
            return _descriptor.Directive<T1>();
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Directive(string name, params ArgumentNode[] arguments)
        {
            return _descriptor.Directive(name, arguments);
        }
    }
}
