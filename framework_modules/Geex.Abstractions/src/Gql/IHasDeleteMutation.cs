using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Geex.Common.Requests;
using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Types;

using MongoDB.Entities;

namespace Geex.Gql;

/// <summary>
/// 提供批量删除的Mutation
/// </summary>
public interface IHasDeleteMutation
{
    /// <summary>
    /// 批量删除, 批量调用Entity的DeleteAsync方法
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public Task<bool> Delete(string[] ids);
}

/// <inheritdoc cref="IHasDeleteMutation"/>
public interface IHasDeleteMutation<T> : IHasDeleteMutation where T : IEntityBase
{
    /// <inheritdoc/>
    Task<bool> IHasDeleteMutation.Delete(string[] ids) => this.Delete(ids);


    /// <inheritdoc cref="IHasDeleteMutation.Delete"/>
    public async Task<bool> Delete(string[] ids, [Service] IUnitOfWork? uow = default)
    {
        if (uow == null) throw new ArgumentNullException(nameof(uow));

        var entities = uow.Query<T>().Where(x => ids.Contains(x.Id)).ToArray();
        await using var _ = uow.StartExplicitTransaction();
        foreach (var entity in entities)
        {
            await entity.DeleteAsync();
        }

        return true;
    }
}

/// <summary>
/// Extensions for IHasDeleteMutation to support field descriptors
/// </summary>
public static class DeleteMutationExtensions
{
    /// <summary>
    /// 为 IHasDeleteMutation 创建删除字段描述符
    /// </summary>
    /// <typeparam name="TMutation">实现 IHasDeleteMutation 的 Mutation 类型</typeparam>
    /// <param name="descriptor">对象类型描述符</param>
    /// <param name="propertyOrMethod">删除方法的表达式</param>
    /// <returns>对象字段描述符</returns>
    public static IObjectFieldDescriptor Field<TMutation>(this IObjectTypeDescriptor<TMutation> descriptor,
        Expression<Func<IHasDeleteMutation, Task<bool>>> propertyOrMethod)
        where TMutation : MutationExtension<TMutation>, IHasDeleteMutation
    {
        var hasDeleteMutationType = typeof(TMutation).GetInterface($"{nameof(IHasDeleteMutation)}`1");
        if (hasDeleteMutationType == null) throw new InvalidOperationException($"Type {typeof(TMutation).Name} does not implement IHasDeleteMutation<T>");

        var entityType = hasDeleteMutationType.GenericTypeArguments[0];
        var entityName = entityType.Name;
        if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
        {
            entityName = entityName[1..];
        }
        var propName = propertyOrMethod.Body.As<MethodCallExpression>().Method.Name.ToCamelCase() + entityName;
        return descriptor.Field(propName);
    }
}
