using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using FastExpressionCompiler;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Entities.Utilities;

namespace Geex
{
    public sealed class Enumeration : Enumeration<IEnumeration>
    {
        public Enumeration([NotNull] string name, string value) : base(name, value)
        {
    }

        public Enumeration(string value) : base(value)
        {
        }
    }
    /// <summary>
    /// A base type to use for creating smart enums.
    /// </summary>
    /// <typeparam name="TEnum">The type that is inheriting from this class.</typeparam>
    /// <typeparam name="string">The type of the inner value.</typeparam>
    /// <remarks></remarks>
    public abstract class Enumeration<TEnum> :
        ValueObject<Enumeration<TEnum>>,
        IEnumeration,
        IEquatable<Enumeration<TEnum>>,
        IComparable<Enumeration<TEnum>>
        where TEnum : class, IEnumeration
    {
        static List<Type>? enumTypes;
        public static List<TEnum> DynamicValues => GetAllOptions().ToList();

        private static ILogger<Enumeration>? _logger = null;
        public static ILogger<Enumeration>? Logger => _logger ??= ServiceLocator.Global?.GetService<ILogger<Enumeration>>();

        public static ConcurrentDictionary<string, TEnum> ValueCacheDictionary { get; } = new ConcurrentDictionary<string, TEnum>();
        static readonly ConcurrentDictionary<string, TEnum> _fromName = new ConcurrentDictionary<string, TEnum>();

        static readonly ConcurrentDictionary<string, TEnum> _fromNameIgnoreCase = new ConcurrentDictionary<string, TEnum>();

        static readonly ConcurrentDictionary<string, TEnum> _fromValue = new ConcurrentDictionary<string, TEnum>();

        private static IEnumerable<TEnum> GetAllOptions()
        {
            enumTypes ??= GeexModule.KnownModuleAssembly
        .Distinct()
        .SelectMany(x => x.DefinedTypes).Where(x => x.IsAssignableTo(typeof(TEnum)) && !x.IsAbstract).Concat(new[] { typeof(TEnum) }).Distinct().ToList();

            List<TEnum> options = new List<TEnum>();
            foreach (Type enumType in enumTypes)
            {
                List<TEnum> typeEnumOptions = enumType.GetPropertiesOfType<TEnum>();
                //var dynamicOptions = enumType.BaseType?.GetField(nameof(Enumeration.DynamicValues))?.GetValue(default) as List<TEnum>;
                //if (dynamicOptions?.Any() == true)
                //{
                //    typeEnumOptions.AddRange(dynamicOptions);
                //}
                options.AddRange(typeEnumOptions);
                foreach (var enumOption in typeEnumOptions)
                {
                    IEnumeration.ValueCacheDictionary.TryAdd($"{typeof(TEnum).Name}.{enumOption.Name}", enumOption);
                    _fromName.TryAdd(enumOption.Name, enumOption);
                    _fromNameIgnoreCase.TryAdd(enumOption.Name.ToLowerInvariant(), enumOption);
                    _fromValue.TryAdd(enumOption.Value, enumOption);
                    ValueCacheDictionary.TryAdd(enumOption.Name, enumOption);
                }

            }

            return options.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Gets a collection containing all the instances of <see cref="Enumeration{TEnum}"/>.
        /// </summary>
        /// <value>A <see cref="IReadOnlyCollection{TEnum}"/> containing all the instances of <see cref="Enumeration{TEnum}"/>.</value>
        /// <remarks>Retrieves all the instances of <see cref="Enumeration{TEnum}"/> referenced by public static read-only fields in the current class or its bases.</remarks>
        public static IEnumerable<TEnum> List =>
            _fromName.Values
                .ToList()
                .AsReadOnly();

        private string _name;
        private string _value;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>A <see cref="String"/> that is the name of the <see cref="Enumeration{TEnum}"/>.</value>
        public string Name =>
            _name;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>A <typeparamref name="string"/> that is the value of the <see cref="Enumeration{TEnum}"/>.</value>
        public string Value =>
            _value;

        string IEnumeration.Value => this.Value;

        public Enumeration(string name, string value) : base((x) => x.Name, x => x.Value)
        {
            SetEnum(name, value);
        }

        internal void SetEnum(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("simple enum value must have a equation of string.");
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (_fromValue.ContainsKey(value))
            {
                throw new InvalidOperationException("Please do use static factory for Enumeration! Enumeration value already exists for this enumeration type: " + typeof(TEnum).Name);
            }

            if (_fromName.ContainsKey(name))
            {
                throw new InvalidOperationException("Please do use static factory for Enumeration! Enumeration name already exists for this enumeration type: " + typeof(TEnum).Name);
            }

            _name = name;
            _value = value;
        }

        /// <summary>
        /// construct a enum with name of value.ToString()
        /// </summary>
        /// <param name="value"></param>
        public Enumeration(string value) : this(value, value)
        {

        }

        public Enumeration()
        {

        }

        /// <summary>
        /// Gets the type of the inner value.
        /// </summary>
        /// <value>A <see name="System.Type"/> that is the type of the value of the <see cref="Enumeration{TEnum}"/>.</value>
        public Type GetValueType() =>
            typeof(string);

        /// <summary>
        /// Gets the item associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the item to get.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case during the comparison; otherwise, <c>false</c>.</param>
        /// <returns>
        /// The item associated with the specified name.
        /// If the specified name is not found, throws a <see cref="KeyNotFoundException"/>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c>.</exception>
        public static TEnum FromName(string name, bool ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (ignoreCase)
                return FromName(_fromNameIgnoreCase);
            else
                return FromName(_fromName);

            TEnum FromName(ConcurrentDictionary<string, TEnum> dictionary)
            {
                if (!dictionary.TryGetValue(name, out var result))
                {
                    // Create dynamic instance instead of throwing exception
                    return Create(name, name);
                }
                return result;
            }
        }

        /// <summary>
        /// Get or create an item associated with the specified value.
        /// </summary>
        /// <param name="value">The value of the item to get.</param>
        /// <returns>
        /// The first item found that is associated with the specified value.
        /// If the specified value is not found, throws a <see cref="KeyNotFoundException"/>.
        /// </returns>
        public static TEnum FromValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!_fromValue.TryGetValue(value, out var result))
            {
                // Create dynamic instance instead of throwing exception
                return Create(value, value);
            }
            return result;
        }

        /// <summary>
        /// Gets an item associated with the specified value.
        /// </summary>
        /// <param name="value">The value of the item to get.</param>
        /// <returns>
        /// The first item found that is associated with the specified value.
        /// If the specified value is not found, throws a <see cref="KeyNotFoundException"/>.
        /// </returns>
        public static TChildEnum FromValue<TChildEnum>(string value) where TChildEnum: class, TEnum
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!_fromValue.TryGetValue(value, out var result))
            {
                // Create dynamic instance instead of throwing exception
                return CreateTyped<TChildEnum>(value, value);
            }

            if (result is TChildEnum childResult)
            {
                return childResult;
            }
            else{
              throw new InvalidOperationException($"Enumeration value '{value}' is not a valid value for type {typeof(TChildEnum).Name}");
            }
        }

        /// <summary>
        /// Gets the item associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the item to get.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case during the comparison; otherwise, <c>false</c>.</param>
        /// <returns>
        /// The item associated with the specified name.
        /// If the specified name is not found, creates a dynamic instance.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c>.</exception>
        public static TChildEnum FromName<TChildEnum>(string name, bool ignoreCase = false) where TChildEnum : class, TEnum
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var dictionary = ignoreCase ? _fromNameIgnoreCase : _fromName;
            if (!dictionary.TryGetValue(name, out var result))
            {
                // Create dynamic instance instead of throwing exception
                return CreateTyped<TChildEnum>(name, name);
            }

            if (result is TChildEnum childResult)
            {
                return childResult;
            }
            else
            {
                throw new InvalidOperationException($"Enumeration name '{name}' is not a valid name for type {typeof(TChildEnum).Name}");
            }
        }

        /// <summary>
        /// Gets the item associated with the specified name and value, throws if either conflicts with existing ones.
        /// </summary>
        /// <param name="name">The name of the item to get or create.</param>
        /// <param name="value">The value of the item to get or create.</param>
        /// <returns>The item associated with the specified name and value.</returns>
        /// <exception cref="ArgumentNullException">If name or value is null or empty.</exception>
        /// <exception cref="InvalidOperationException">If name or value already exists but with a different value or name.</exception>
        public static TEnum FromNameAndValue(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // 优先通过Value查找
            var byValue = FromExistedValue(value, true);
            if (byValue != null && byValue.Name != name)
            {
                throw new InvalidOperationException($"Enumeration value '{value}' already exists with a different name '{byValue.Name}'.");
            }
            if (byValue != null)
            {
                return byValue;
            }

            // 再通过Name查找
            var byName = FromExistedName(name, false, true);
            if (byName != null && byName.Value != value)
            {
                throw new InvalidOperationException($"Enumeration name '{name}' already exists with a different value '{byName.Value}'.");
            }
            if (byName != null)
            {
                return byName;
            }

            // 都不存在则创建
            return Create(name, value);
        }

        /// <summary>
        /// Gets the item associated with the specified name and value, throws if either conflicts with existing ones.
        /// </summary>
        /// <param name="name">The name of the item to get or create.</param>
        /// <param name="value">The value of the item to get or create.</param>
        /// <returns>The item associated with the specified name and value.</returns>
        /// <exception cref="ArgumentNullException">If name or value is null or empty.</exception>
        /// <exception cref="InvalidOperationException">If name or value already exists but with a different value or name.</exception>
        public static TChildEnum FromNameAndValue<TChildEnum>(string name, string value) where TChildEnum : class, TEnum
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // 优先通过Value查找
            var byValue = FromExistedValue(value, true);
            if (byValue != null && byValue.Name != name)
            {
                throw new InvalidOperationException($"Enumeration value '{value}' already exists with a different name '{byValue.Name}'.");
            }
            if (byValue != null)
            {
                if (byValue is TChildEnum childResult)
                {
                    return childResult;
                }
                else
                {
                    throw new InvalidOperationException($"Enumeration value '{value}' is not a valid value for type {typeof(TChildEnum).Name}");
                }
            }

            // 再通过Name查找
            var byName = FromExistedName(name, false, true);
            if (byName != null && byName.Value != value)
            {
                throw new InvalidOperationException($"Enumeration name '{name}' already exists with a different value '{byName.Value}'.");
            }
            if (byName != null)
            {
                if (byName is TChildEnum childResult)
                {
                    return childResult;
                }
                else
                {
                    throw new InvalidOperationException($"Enumeration name '{name}' is not a valid name for type {typeof(TChildEnum).Name}");
                }
            }

            // 都不存在则创建
            return CreateTyped<TChildEnum>(name, value);
        }

        /// <summary>
        /// Gets the item associated with the specified name, throws if not found.
        /// </summary>
        /// <param name="name">The name of the item to get.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case during the comparison; otherwise, <c>false</c>.</param>
        /// <param name="noException">If true, returns null instead of throwing exception when not found.</param>
        /// <returns>The item associated with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">If the specified name is not found and noException is false.</exception>
        public static TChildEnum FromExistedName<TChildEnum>(string name, bool ignoreCase = false, bool noException = false) where TChildEnum : class, TEnum
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var dictionary = ignoreCase ? _fromNameIgnoreCase : _fromName;
            if (!dictionary.TryGetValue(name, out var result))
            {
                if (noException)
                {
                    return null;
                }
                throw new KeyNotFoundException($"Enumeration name '{name}' does not exist.");
            }

            if (result is TChildEnum childResult)
            {
                return childResult;
            }
            else
            {
                if (noException)
                {
                    return null;
                }
                throw new InvalidOperationException($"Enumeration name '{name}' is not a valid name for type {typeof(TChildEnum).Name}");
            }
        }

        /// <summary>
        /// Gets the item associated with the specified name, throws if not found.
        /// </summary>
        /// <param name="name">The name of the item to get.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case during the comparison; otherwise, <c>false</c>.</param>
        /// <param name="noException">If true, returns null instead of throwing exception when not found.</param>
        /// <returns>The item associated with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">If the specified name is not found and noException is false.</exception>
        public static TEnum FromExistedName(string name, bool ignoreCase = false, bool noException = false)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var dictionary = ignoreCase ? _fromNameIgnoreCase : _fromName;
            if (!dictionary.TryGetValue(name, out var result))
            {
                if (noException)
                {
                    return null;
                }
                throw new KeyNotFoundException($"Enumeration name '{name}' does not exist.");
            }

            return result;
        }

        /// <summary>
        /// Gets the item associated with the specified value, throws if not found.
        /// </summary>
        /// <param name="value">The value of the item to get.</param>
        /// <param name="noException">If true, returns null instead of throwing exception when not found.</param>
        /// <returns>The item associated with the specified value.</returns>
        /// <exception cref="KeyNotFoundException">If the specified value is not found and noException is false.</exception>
        public static TChildEnum FromExistedValue<TChildEnum>(string value, bool noException = false) where TChildEnum : class, TEnum
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!_fromValue.TryGetValue(value, out var result))
            {
                if (noException)
                {
                    return null;
                }
                throw new KeyNotFoundException($"Enumeration value '{value}' does not exist.");
            }

            if (result is TChildEnum childResult)
            {
                return childResult;
            }
            else
            {
                if (noException)
                {
                    return null;
                }
                throw new InvalidOperationException($"Enumeration value '{value}' is not a valid value for type {typeof(TChildEnum).Name}");
            }
        }

        /// <summary>
        /// Gets the item associated with the specified value, throws if not found.
        /// </summary>
        /// <param name="value">The value of the item to get.</param>
        /// <param name="noException">If true, returns null instead of throwing exception when not found.</param>
        /// <returns>The item associated with the specified value.</returns>
        /// <exception cref="KeyNotFoundException">If the specified value is not found and noException is false.</exception>
        public static TEnum FromExistedValue(string value, bool noException = false)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!_fromValue.TryGetValue(value, out var result))
            {
                if (noException)
                {
                    return null;
                }
                throw new KeyNotFoundException($"Enumeration value '{value}' does not exist.");
            }

            return result;
        }

        public override string ToString() => _name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() =>
            _value.GetHashCode();

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) =>
            (obj is Enumeration<TEnum> other) && Equals(other);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="Enumeration{TEnum}"/> value.
        /// </summary>
        /// <param name="other">An <see cref="Enumeration{TEnum}"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public virtual bool Equals(Enumeration<TEnum> other)
        {
            // check if same instance
            if (Object.ReferenceEquals(this, other))
                return true;

            // it's not same instance so
            // check if it's not null and is same value
            if (other is null)
                return false;

            return _value.Equals(other._value);
        }

        public static bool operator ==(Enumeration<TEnum> left, Enumeration<TEnum>? right)
        {
            // Handle null on left side
            if (left is null)
                return right is null; // null == null = true

            // Equals handles null on right side
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
            !(left == right);

        /// <summary>
        /// Compares this instance to a specified <see cref="Enumeration{TEnum}"/> and returns an indication of their relative values.
        /// </summary>
        /// <param name="other">An <see cref="Enumeration{TEnum}"/> value to compare to this instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and <paramref name="other"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int CompareTo(Enumeration<TEnum> other) =>
            _value.CompareTo(other._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Enumeration<TEnum> left, Enumeration<TEnum> right) =>
            left.CompareTo(right) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Enumeration<TEnum> left, Enumeration<TEnum> right) =>
            left.CompareTo(right) <= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Enumeration<TEnum> left, Enumeration<TEnum> right) =>
            left.CompareTo(right) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Enumeration<TEnum> left, Enumeration<TEnum> right) =>
            left.CompareTo(right) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(Enumeration<TEnum> enumeration) =>
            enumeration._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Enumeration<TEnum>(string value) =>
            FromValue(value) as Enumeration<TEnum>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Enumeration<TEnum>((string name, string value) tuple) =>
            Create(tuple.name, tuple.value) as Enumeration<TEnum>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TEnum(Enumeration<TEnum> value) =>
            FromValue(value);

        /// <summary>
        /// Creates a dynamic enumeration instance when no existing instance is found
        /// </summary>
        private static TEnum Create(string name, string value)
        {
            if (_fromValue.TryGetValue(value, out var existed))
            {
                if (existed.Name != name)
                {
                    Logger?.LogWarning($"Enumeration value '{value}' already exists with a different name '{existed.Name}'.");
                }
                return existed;
            }

            if (_fromName.TryGetValue(name, out existed))
            {
                if (existed.Value != value)
                {
                    Logger?.LogWarning($"Enumeration name '{name}' already exists with a different value '{existed.Value}'.");
                }
                return existed;
            }

            // Try to create an instance of the concrete enumeration type using high-performance cache
            var concreteType = typeof(TEnum);
            try
            {
                var instance = (TEnum)concreteType.CreateInstanceFast();
                (instance as Enumeration<TEnum>).SetEnum(name, value);
                // Cache the new instance
                _fromName.TryAdd(name, instance);
                _fromNameIgnoreCase.TryAdd(name, instance);
                _fromValue.TryAdd(value, instance);
                ValueCacheDictionary.TryAdd(name, instance);
                IEnumeration.ValueCacheDictionary.TryAdd($"{typeof(TEnum).Name}.{name}", instance);

                return instance;
            }
            catch
            {
                // If we can't create an instance, fall back to throwing the original exception
                throw new InvalidOperationException($"Cannot create enumeration with name '{name}' and value '{value}'");
            }
        }

        /// <summary>
        /// Creates a dynamic enumeration instance of specific child type when no existing instance is found
        /// </summary>
        private static TChildEnum CreateTyped<TChildEnum>(string name, string value) where TChildEnum : class, TEnum
        {
            // First check if it already exists in the cache
            if (_fromValue.TryGetValue(value, out var existed))
            {
                if (existed.Name != name)
                {
                    Logger?.LogWarning($"Enumeration value '{value}' already exists with a different name '{existed.Name}'.");
                }
                if (existed is TChildEnum childExisted)
                {
                    return childExisted;
                }
                else
                {
                    throw new InvalidOperationException($"Enumeration value '{value}' exists but is not of type {typeof(TChildEnum).Name}");
                }
            }

            if (_fromName.TryGetValue(name, out existed))
            {
                if (existed.Value != value)
                {
                    Logger?.LogWarning($"Enumeration name '{name}' already exists with a different value '{existed.Value}'.");
                }
                if (existed is TChildEnum childExisted)
                {
                    return childExisted;
                }
                else
                {
                    throw new InvalidOperationException($"Enumeration name '{name}' exists but is not of type {typeof(TChildEnum).Name}");
                }
            }

            // Try to create an instance of the concrete enumeration type using high-performance cache
            var concreteType = typeof(TChildEnum);
            try
            {
                var instance = (TChildEnum)concreteType.CreateInstanceFast();
                (instance as Enumeration<TEnum>).SetEnum(name, value);
                // Cache the new instance
                _fromName.TryAdd(name, instance);
                _fromNameIgnoreCase.TryAdd(name, instance);
                _fromValue.TryAdd(value, instance);
                ValueCacheDictionary.TryAdd(name, instance);
                IEnumeration.ValueCacheDictionary.TryAdd($"{typeof(TChildEnum).Name}.{name}", instance);

                return instance;
            }
            catch
            {
                // If we can't create an instance, fall back to throwing the original exception
                throw new InvalidOperationException($"Cannot create enumeration with name '{name}' and value '{value}' of type {typeof(TChildEnum).Name}");
            }
        }
    }

    public static class EnumerationExtensions
    {
        public static IEnumerable<Type> GetClassEnumBases(this Type classEnumType)
        {
            return classEnumType.GetBaseClasses().Where(x => !x.IsGenericType && x.IsAssignableTo<IEnumeration>());
        }

        public static Type GetClassEnumRealType(this Type type)
        {
            return type.GetBaseClasses(false).First(x => x.IsAssignableTo<IEnumeration>()).GenericTypeArguments[0];
        }

    }
}
