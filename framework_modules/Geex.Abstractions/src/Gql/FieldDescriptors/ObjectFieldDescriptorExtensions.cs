using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// 对象类型描述符的强类型扩展方法
/// </summary>
public static class ObjectTypeDescriptorExtensions
{
    /// <summary>
    /// 增强版 Field 方法，返回强类型的字段描述符
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="descriptor">对象类型描述符</param>
    /// <param name="property">字段表达式</param>
    /// <returns>强类型的字段描述符</returns>
    public static ObjectFieldDescriptor<T, TValue> TypedField<T, TValue>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, TValue>> property)
    {
        var field = descriptor.Field(property);
        return new ObjectFieldDescriptor<T, TValue>(field);
    }
}

/// <summary>
/// 强类型对象字段描述符的扩展方法
/// </summary>
public static class ObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor<T, TValue> Authorize<T, TValue>(
        this IObjectFieldDescriptor<T, TValue> descriptor)
    {
        return (ObjectFieldDescriptor<T, TValue>)((descriptor as IObjectFieldDescriptor).Authorize());
    }
    /// <summary>
    /// 为强类型字段描述符添加偏移分页支持，自动推断分页类型
    /// </summary>
    /// <typeparam name="T">父类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="descriptor">强类型字段描述符</param>
    /// <returns>字段描述符</returns>
    public static IObjectFieldDescriptor<T, TValue> UseOffsetPaging<T, TValue>(
        this IObjectFieldDescriptor<T, TValue> descriptor)
    {
        // 获取TValue的元素类型（如果是IQueryable<TItem>、IEnumerable<TItem>等）
        var elementType = GetElementType(typeof(TValue));

        if (elementType != null)
        {
            // 调用标准的UseOffsetPaging扩展方法
            // 由于我们有隐式转换，可以直接将强类型描述符传递给原始扩展方法
            var baseDescriptor = (IObjectFieldDescriptor)descriptor;

            // 根据元素类型决定使用ObjectType还是InterfaceType
            if (elementType.IsInterface)
            {
                // 对于接口类型，使用InterfaceType<T>
                baseDescriptor.UseOffsetPaging<InterfaceType<TValue>>();
            }
            else
            {
                // 对于具体类型，使用ObjectType<T>
                baseDescriptor.UseOffsetPaging<ObjectType<TValue>>();
            }
        }

        return descriptor;
    }

    /// <summary>
    /// 为强类型字段描述符添加过滤支持，自动推断过滤类型
    /// </summary>
    /// <typeparam name="T">父类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="descriptor">强类型字段描述符</param>
    /// <returns>字段描述符</returns>
    public static IObjectFieldDescriptor<T, TValue> UseFiltering<T, TValue>(
        this IObjectFieldDescriptor<T, TValue> descriptor)
    {
        // 获取TValue的元素类型
        var elementType = GetElementType(typeof(TValue));

        if (elementType != null)
        {
            var baseDescriptor = (IObjectFieldDescriptor)descriptor;
            baseDescriptor.UseFiltering<TValue>();
        }

        return descriptor;
    }

    /// <summary>
    /// 为强类型字段描述符添加排序支持，自动推断排序类型
    /// </summary>
    /// <typeparam name="T">父类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="descriptor">强类型字段描述符</param>
    /// <returns>字段描述符</returns>
    public static IObjectFieldDescriptor<T, TValue> UseSorting<T, TValue>(
        this IObjectFieldDescriptor<T, TValue> descriptor)
    {
        // 获取TValue的元素类型
        var elementType = GetElementType(typeof(TValue));

        if (elementType != null)
        {
            var baseDescriptor = (IObjectFieldDescriptor)descriptor;
            baseDescriptor.UseSorting<TValue>();
        }

        return descriptor;
    }

    /// <summary>
    /// 获取集合类型的元素类型
    /// </summary>
    /// <param name="type">集合类型</param>
    /// <returns>元素类型，如果不是集合类型则返回null</returns>
    private static Type GetElementType(Type type)
    {
        // 处理Task<T>包装
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            type = type.GetGenericArguments()[0];
        }

        // 处理IQueryable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            return type.GetGenericArguments()[0];
        }

        // 处理IEnumerable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        // 处理实现了IQueryable<T>的类型
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        // 处理实现了IEnumerable<T>的类型
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        // 如果是数组类型
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return null;
    }
}
