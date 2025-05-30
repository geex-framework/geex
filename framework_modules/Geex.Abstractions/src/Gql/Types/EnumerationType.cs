﻿using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.Types
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
