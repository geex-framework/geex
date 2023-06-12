using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Common.Abstractions;

using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace Geex.Common.Gql
{
    public class GeexTypeInspector : DefaultTypeInspector
    {
        public Dictionary<Type, IExtendedType> KnownTypes { get; } = new();

        protected override void Initialize(IConventionContext context)
        {
            base.Initialize(context);
        }

        

        /// <inheritdoc />
        public override IExtendedType GetReturnType(MemberInfo member, bool ignoreAttributes = false)
        {
            IExtendedType result;
            if (member is PropertyInfo property)
            {
                if (property.PropertyType.Name is "ResettableLazy`1" or "Lazy`1")
                {
                    result = base.GetType(property.PropertyType.GenericTypeArguments[0]);
                }
                else
                {
                    result = base.GetReturnType(member, ignoreAttributes);
                }
            }
            else
            {
                result = base.GetReturnType(member, ignoreAttributes);
            }

            if (member.DeclaringType != null)
            {
                this.KnownTypes[member.DeclaringType] = result;
            }
            return result;
        }

        //public override IEnumerable<MemberInfo> GetMembers(Type type)
        //{
        //    return base.GetMembers(type);
        //}

        public override IEnumerable<object> GetEnumValues(Type enumType)
        {
            if ((object)enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (enumType != typeof(object) && enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<object>();
            }

            if (enumType.IsAssignableTo<IEnumeration>())
            {
                var genericImplementation = enumType.GetBaseClasses().FirstOrDefault(x => x.Name == (typeof(Enumeration<>).Name));
                var values = ((System.Collections.IEnumerable)genericImplementation?.GetProperty(nameof(Enumeration.List))?.GetValue(null)).Cast<object>().Where(x => x.GetType().IsAssignableTo(enumType));
                if (!values.Any())
                {
                    Console.WriteLine("enum no values");
                }
                return values;
            }
            return Enumerable.Empty<object>();
        }

        protected override void Complete(IConventionContext context)
        {
            base.Complete(context);
        }
    }
}
