using HotChocolate.Authorization;
using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>Authorize extensions for the object field descriptor.</summary>
public static class AuthorizeObjectFieldDescriptorExtensions
{
    /// <summary>Adds authorization to a field.</summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="T:HotChocolate.Types.IObjectFieldDescriptor" /> for configuration chaining.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// The <paramref name="descriptor" /> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor<T, TValue> Authorize<T, TValue>(
      this IObjectFieldDescriptor<T, TValue> descriptor,
      ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        return (ObjectFieldDescriptor<T, TValue>)(descriptor as IObjectFieldDescriptor).Authorize(apply);
    }

    /// <summary>Adds authorization to a field.</summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="policy">The authorization policy name.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="T:HotChocolate.Types.IObjectFieldDescriptor" /> for configuration chaining.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// The <paramref name="descriptor" /> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor<T, TValue> Authorize<T, TValue>(
      this IObjectFieldDescriptor<T, TValue> descriptor,
      string policy,
      ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        return (ObjectFieldDescriptor<T, TValue>)(descriptor as IObjectFieldDescriptor).Authorize(policy, apply: apply);
    }

    /// <summary>Adds authorization to a field.</summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="roles">The roles for which this field shall be accessible.</param>
    /// <returns>
    /// Returns the <see cref="T:HotChocolate.Types.IObjectFieldDescriptor" /> for configuration chaining.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// The <paramref name="descriptor" /> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor<T, TValue> Authorize<T, TValue>(
      this IObjectFieldDescriptor<T, TValue> descriptor,
      params string[] roles)
    {
        return (ObjectFieldDescriptor<T, TValue>)(descriptor as IObjectFieldDescriptor).Authorize(roles);
    }

    /// <summary>Allows anonymous access to this field.</summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <returns>
    ///  Returns the <see cref="T:HotChocolate.Types.IObjectFieldDescriptor" /> for configuration chaining.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// The <paramref name="descriptor" /> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor<T, TValue> AllowAnonymous<T, TValue>(this IObjectFieldDescriptor<T, TValue> descriptor)
    {
        return (ObjectFieldDescriptor<T, TValue>)(descriptor as IObjectFieldDescriptor).AllowAnonymous();
    }
}
