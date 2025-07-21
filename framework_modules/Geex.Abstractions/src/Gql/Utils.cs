using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Geex;
using HotChocolate.Language;

class Utils
{
    // todo: This is a utility class for creating GraphQL value nodes from various types of objects, can be migrated to use HotChocolate's built-in utilities.
    public static IValueNode CreateValueNode(object value)
    {
        if (value == null)
            return NullValueNode.Default;

        return value switch
        {
            string str => new StringValueNode(str),
            int intVal => new IntValueNode(intVal),
            long longVal => new IntValueNode(longVal),
            float floatVal => new FloatValueNode(floatVal),
            double doubleVal => new FloatValueNode(doubleVal),
            decimal decimalVal => new FloatValueNode(decimalVal),
            bool boolVal => new BooleanValueNode(boolVal),
            Enum enumVal => new EnumValueNode(enumVal.ToString()),
            IEnumeration enumVal => new EnumValueNode(enumVal.ToString()),
            IEnumerable<object> listVal => new ListValueNode(listVal.Select(CreateValueNode).ToArray()),
            _ when IsComplexObject(value) => CreateObjectValueNode(value),
            _ => new StringValueNode(value.ToString())
        };
    }

    private static bool IsComplexObject(object value)
    {
        var type = value.GetType();
        return !type.IsPrimitive && type != typeof(string) && type != typeof(decimal) && type.IsAssignableTo<IEnumeration>();
    }

    private static ObjectValueNode CreateObjectValueNode(object obj)
    {
        var fields = new List<ObjectFieldNode>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                var propValue = prop.GetValue(obj);
                var fieldName = prop.Name.ToCamelCase();
                var valueNode = CreateValueNode(propValue);
                fields.Add(new ObjectFieldNode(fieldName, valueNode));
            }
        }

        return new ObjectValueNode(fields);
    }
}