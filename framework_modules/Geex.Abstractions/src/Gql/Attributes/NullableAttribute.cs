using System;
using System.Reflection;
using Geex.Gql.Extensions;
using HotChocolate.Types;

using HotChocolate.Types.Descriptors;

namespace Geex.Gql.Attributes
{
    [AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter,
    Inherited = true,
    AllowMultiple = true)]
    public sealed class NullableAttribute : DescriptorAttribute
    {
        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            switch (element)
            {
                case MemberInfo member:
                    switch (descriptor)
                    {
                        case IInterfaceFieldDescriptor interfaceField:
                            interfaceField.MakeNullable();
                            break;
                        case IObjectFieldDescriptor objectField:
                            objectField.MakeNullable();
                            break;
                        case IInputFieldDescriptor inputField:
                            inputField.MakeNullable();
                            break;
                    }

                    break;
                case ParameterInfo parameter:
                    {
                        if (descriptor is IArgumentDescriptor argumentDescriptor)
                        {
                            argumentDescriptor.MakeNullable();
                        }

                        break;
                    }
            }
        }
    }

}
