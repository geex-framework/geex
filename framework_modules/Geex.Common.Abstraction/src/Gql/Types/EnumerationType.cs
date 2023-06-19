using System;
using System.Linq;

using Geex.Common.Abstractions;

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Common.Gql.Types
{
    public class EnumerationType<TEnum> : EnumType<TEnum> where TEnum : class, IEnumeration
    {
        protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            var enumTypeDefinition = base.CreateDefinition(context);
            return enumTypeDefinition;
        }

        protected override bool TryCreateEnumValue(ITypeCompletionContext context, EnumValueDefinition definition, out IEnumValue? enumValue)
        {
            definition.Name = definition.RuntimeValue.ToString();
            var tryCreateEnumValue = base.TryCreateEnumValue(context, definition, out enumValue);
            return tryCreateEnumValue;
        }

    }
}
