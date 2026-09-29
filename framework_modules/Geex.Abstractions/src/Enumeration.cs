using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    /// <remarks></remarks>
    public abstract class Enumeration<TEnum> :
        ValueObject<Enumeration<TEnum>>,
        IEnumeration,
        IEquatable<Enumeration<TEnum>>,
        IComparable<Enumeration<TEnum>>
        where TEnum : class, IEnumeration
    {
        private static readonly object _cacheLock = new();
        private static HashSet<Assembly> _knownAssemblies = new();
        private static Type[] _enumTypes = Array.Empty<Type>();
        private static readonly ConcurrentDictionary<Type, byte> _initializedTypes = new();
        [ThreadStatic] private static bool _initializing;
        [ThreadStatic] private static HashSet<Type>? _initializingTypes;
        public static List<TEnum> DynamicValues => GetAllOptions().ToList();

        private static ILogger<Enumeration>? _logger = null;
        public static ILogger<Enumeration>? Logger => _logger ??= ServiceLocator.Global?.GetService<ILogger<Enumeration>>();

        public static ConcurrentDictionary<string, TEnum> ValueCacheDictionary { get; } = new ConcurrentDictionary<string, TEnum>();
        static readonly ConcurrentDictionary<string, TEnum> _fromName = new ConcurrentDictionary<string, TEnum>();

        static readonly ConcurrentDictionary<string, TEnum> _fromNameIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

        static readonly ConcurrentDictionary<string, TEnum> _fromValue = new ConcurrentDictionary<string, TEnum>();
        static readonly ConcurrentDictionary<string, string> _aliasToValue = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<Type, Func<string, string, TEnum>> _constructorCache = new();

        private static IEnumerable<TEnum> GetAllOptions()
        {
            EnsureInitialized(typeof(TEnum));
            return _fromName.Values.OrderBy(t => t.Name).ToList();
        }

        private static void EnsureInitialized(Type requestedType, Func<bool>? isResolved = null)
        {
            if (requestedType.GetEnumerationFamilyType() != typeof(Enumeration<TEnum>))
                throw new InvalidOperationException($"Type {requestedType.FullName} belongs to a different enumeration family.");
            // Static member initializers call FromValue themselves. They must be able to
            // construct their member without recursively discovering unfinished members.
            if (_initializing)
            {
                InitializeType(requestedType, isResolved);
                return;
            }
            _initializing = true;
            try
            {
                var assemblies = GeexModule.KnownModuleAssembly.ToHashSet();
                Type[] types;
                lock (_cacheLock)
                {
                    if (!_knownAssemblies.SetEquals(assemblies))
                    {
                        _enumTypes = assemblies.SelectMany(x => x.DefinedTypes)
                            .Where(x => typeof(TEnum).IsAssignableFrom(x) && !x.ContainsGenericParameters &&
                                x.GetEnumerationFamilyType() == typeof(Enumeration<TEnum>))
                            .Select(x => (Type)x).ToArray();
                        _knownAssemblies = assemblies;
                    }
                    types = _enumTypes;
                }

                foreach (var type in new[] { typeof(TEnum), requestedType }.Concat(types).Distinct())
                {
                    InitializeType(type, isResolved);
                }
            }
            finally
            {
                _initializing = false;
            }
        }

        private static void InitializeType(Type type, Func<bool>? isResolved)
        {
            if (_initializedTypes.ContainsKey(type))
                return;
            var activeTypes = _initializingTypes ??= new HashSet<Type>();
            if (!activeTypes.Add(type))
            {
                RegisterInitializedStorage(type);
                return;
            }
            try
            {
                // Never wait for a type initializer while holding the cache lock:
                // another thread's initializer may itself need to publish a member.
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                RegisterInitializedStorage(type);
                // A lookup can be made by a cctor started outside discovery. Assigned
                // storage can satisfy it without evaluating still-dependent getters.
                lock (_cacheLock)
                {
                    if (isResolved?.Invoke() == true)
                        return;
                }
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                var members = type.GetProperties(flags)
                    .Where(p => p.GetMethod is { IsPublic: true, IsStatic: true } &&
                        typeof(TEnum).IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0)
                    .Select(p => (TEnum?)p.GetValue(null))
                    .Concat(type.GetFields(flags)
                        .Where(f => f.IsInitOnly && typeof(TEnum).IsAssignableFrom(f.FieldType))
                        .Select(f => (TEnum?)f.GetValue(null))).ToArray();
                foreach (var member in members)
                {
                    if (member is null)
                        continue;
                    lock (_cacheLock)
                    {
                        RegisterInstance(member);
                    }
                }
                // A user-started cctor may still have unassigned static members.
                if (members.All(member => member is not null))
                    _initializedTypes.TryAdd(type, 0);
            }
            finally
            {
                activeTypes.Remove(type);
            }
        }

        private static void RegisterInitializedStorage(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var fields = type.GetFields(flags)
                .Where(f => f.IsInitOnly && typeof(TEnum).IsAssignableFrom(f.FieldType));
            var backingFields = type.GetProperties(flags)
                .Where(p => p.GetMethod is { IsPublic: true, IsStatic: true } &&
                    typeof(TEnum).IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0)
                .Select(p => p.DeclaringType!.GetField($"<{p.Name}>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .OfType<FieldInfo>()
                .Where(f => f.IsDefined(typeof(CompilerGeneratedAttribute), false));

            // Reading assigned storage avoids recursively invoking computed getters during a cctor.
            foreach (var field in fields.Concat(backingFields))
            {
                if (field.GetValue(null) is TEnum member)
                {
                    lock (_cacheLock)
                    {
                        RegisterInstance(member);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a collection containing all the instances of <see cref="Enumeration{TEnum}"/>.
        /// </summary>
        /// <value>A <see cref="IReadOnlyCollection{TEnum}"/> containing all the instances of <see cref="Enumeration{TEnum}"/>.</value>
        /// <remarks>Includes discovered static members and dynamically created instances in this enumeration family.</remarks>
        public static IEnumerable<TEnum> List => GetAllOptions();

        private string _name = null!;
        private string _value = null!;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>A <see cref="String"/> that is the name of the <see cref="Enumeration{TEnum}"/>.</value>
        public string Name =>
            _name;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The string identifying this member.</value>
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
                throw new InvalidOperationException("Use FromValue or FromNameAndValue to reuse an existing enumeration value: " + typeof(TEnum).Name);
            }

            if (_fromName.ContainsKey(name))
            {
                throw new InvalidOperationException("Use FromName or FromNameAndValue to reuse an existing enumeration name: " + typeof(TEnum).Name);
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
        /// If the name is absent, creates a member using the name as its value.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c>.</exception>
        public static TEnum FromName(string name, bool ignoreCase = false) =>
            ResolveName(typeof(TEnum), name, ignoreCase);

        /// <summary>
        /// Resolves a name as the requested subtype, rejecting an incompatible cached instance.
        /// </summary>
        public static TChildEnum FromName<TChildEnum>(string name, bool ignoreCase = false)
            where TChildEnum : class, TEnum =>
            (TChildEnum)ResolveName(typeof(TChildEnum), name, ignoreCase);

        private static TEnum ResolveName(Type requestedType, string name, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            var dictionary = ignoreCase ? _fromNameIgnoreCase : _fromName;
            EnsureInitialized(requestedType, () => dictionary.ContainsKey(name));
            return dictionary.TryGetValue(name, out var result)
                ? ReuseInstance(result, requestedType)
                : GetOrCreate(requestedType, name, name);
        }

        /// <summary>
        /// Reuses the family's existing instance for a value, creating it only when absent.
        /// A compatible instance of a different runtime type is reused with a warning.
        /// </summary>
        public static TEnum FromValue(string value) => FromValue(value, []);

        /// <summary>
        /// Resolves a value and associates aliases with its canonical instance.
        /// An alias already associated with another instance is a conflict.
        /// </summary>
        public static TEnum FromValue(string value, params string[] aliases)
        {
            ArgumentNullException.ThrowIfNull(aliases);
            var item = ResolveValue(typeof(TEnum), value);
            var pendingAliases = aliases.Where(alias => !string.IsNullOrWhiteSpace(alias) && alias != item.Value)
                .Distinct(StringComparer.Ordinal).ToArray();
            lock (_cacheLock)
            {
                foreach (var alias in pendingAliases)
                {
                    if (_fromValue.TryGetValue(alias, out var existing) && !ReferenceEquals(existing, item))
                        throw new InvalidOperationException($"Enumeration alias '{alias}' already refers to value '{existing.Value}'.");
                }
                foreach (var alias in pendingAliases)
                {
                    _aliasToValue[alias] = item.Value;
                    _fromValue[alias] = item;
                }
            }
            return item;
        }

        /// <summary>
        /// Resolves a value as the requested subtype, rejecting an incompatible cached instance.
        /// </summary>
        public static TChildEnum FromValue<TChildEnum>(string value) where TChildEnum : class, TEnum =>
            (TChildEnum)ResolveValue(typeof(TChildEnum), value);

        private static TEnum ResolveValue(Type requestedType, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            EnsureInitialized(requestedType, () => _fromValue.ContainsKey(value));
            if (_aliasToValue.TryGetValue(value, out var canonical))
                value = canonical;
            return _fromValue.TryGetValue(value, out var result)
                ? ReuseInstance(result, requestedType)
                : GetOrCreate(requestedType, value, value);
        }

        internal static TEnum ResolveSerialized(Type requestedType, string token)
        {
            ArgumentNullException.ThrowIfNull(token);
            EnsureInitialized(requestedType, () => FindSerialized(token) is not null);
            TEnum? existing;
            lock (_cacheLock)
            {
                existing = FindSerialized(token);
            }
            if (existing is not null)
                return ReuseInstance(existing, requestedType);
            return GetOrCreate(requestedType, token, token, serializedToken: token);
        }

        private static TEnum? FindSerialized(string token)
        {
            _fromValue.TryGetValue(token, out var byValue);
            _fromName.TryGetValue(token, out var byName);
            if (byValue is not null && byName is not null && !ReferenceEquals(byValue, byName))
                throw new JsonException($"Enumeration token '{token}' matches different members by value and name.");
            return byValue ?? byName;
        }

        /// <summary>
        /// Reuses an existing value, preserving its original name, or creates a new member.
        /// A name already associated with a different value is a conflict.
        /// </summary>
        public static TEnum FromNameAndValue(string name, string value) =>
            Create(name, value);

        /// <summary>
        /// Reuses a compatible existing value with its original name, or creates the requested subtype.
        /// A conflicting name or incompatible cached type is rejected.
        /// </summary>
        public static TChildEnum FromNameAndValue<TChildEnum>(string name, string value)
            where TChildEnum : class, TEnum =>
            (TChildEnum)GetOrCreate(typeof(TChildEnum), name, value);

        /// <summary>
        /// Finds an existing compatible member by name without dynamically creating one.
        /// With noException, a missing or incompatible member returns null.
        /// </summary>
        public static TChildEnum FromExistedName<TChildEnum>(string name, bool ignoreCase = false, bool noException = false)
            where TChildEnum : class, TEnum =>
            (TChildEnum)FindExisting(typeof(TChildEnum), name, true, ignoreCase, noException);

        /// <summary>
        /// Finds an existing member by name without dynamically creating one.
        /// With noException, a missing member returns null.
        /// </summary>
        public static TEnum FromExistedName(string name, bool ignoreCase = false, bool noException = false) =>
            FindExisting(typeof(TEnum), name, true, ignoreCase, noException);

        /// <summary>
        /// Finds an existing compatible member by value without dynamically creating one.
        /// With noException, a missing or incompatible member returns null.
        /// </summary>
        public static TChildEnum FromExistedValue<TChildEnum>(string value, bool noException = false)
            where TChildEnum : class, TEnum =>
            (TChildEnum)FindExisting(typeof(TChildEnum), value, false, false, noException);

        /// <summary>
        /// Finds an existing member by value without dynamically creating one.
        /// With noException, a missing member returns null.
        /// </summary>
        public static TEnum FromExistedValue(string value, bool noException = false) =>
            FindExisting(typeof(TEnum), value, false, false, noException);

        private static TEnum FindExisting(Type requestedType, string key, bool byName, bool ignoreCase, bool noException)
        {
            if (key is null || (byName && key.Length == 0))
                throw new ArgumentNullException(byName ? "name" : "value");
            var dictionary = byName ? (ignoreCase ? _fromNameIgnoreCase : _fromName) : _fromValue;
            EnsureInitialized(requestedType, () => dictionary.ContainsKey(key));
            if (dictionary.TryGetValue(key, out var result))
            {
                if (noException && !requestedType.IsInstanceOfType(result))
                    return null;
                return ReuseInstance(result, requestedType);
            }
            if (noException)
                return null;
            throw new KeyNotFoundException($"Enumeration {(byName ? "name" : "value")} '{key}' does not exist.");
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
        private static TEnum Create(string name, string value) =>
            GetOrCreate(typeof(TEnum), name, value);

        private static readonly ConcurrentDictionary<string, Lazy<TEnum>> _creationSlots = new();

        private protected static TEnum DefineInstance<TDetails>(Type requestedType, string name, string value, TDetails details)
        {
            ArgumentNullException.ThrowIfNull(details);
            if (!typeof(Enumeration<TEnum, TDetails>).IsAssignableFrom(requestedType))
                throw new InvalidOperationException($"Type {requestedType.FullName} does not support these enumeration details.");
            return GetOrCreate(requestedType, name, value, instance =>
                ((Enumeration<TEnum, TDetails>)(object)instance).CompleteDefinition(details));
        }

        internal virtual void WarnIfIncomplete() { }

        private static TEnum GetOrCreate(Type requestedType, string name, string value,
            Action<TEnum>? completeDefinition = null, string? serializedToken = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            EnsureInitialized(requestedType, () => FindCandidate() is not null);
            TEnum? existing;
            lock (_cacheLock)
            {
                existing = FindCandidate();
                ThrowIfNameConflicts(name, existing?.Value ?? value);
            }
            if (existing is not null)
                return Finish(existing);

            // A shared slot prevents competing callers from constructing the same value twice.
            // User construction runs outside the family lock; only publication holds it.
            var slot = _creationSlots.GetOrAdd(value, _ => new Lazy<TEnum>(() =>
            {
                lock (_cacheLock)
                {
                    var cached = FindCandidate();
                    if (cached is not null)
                        return cached;
                    ThrowIfNameConflicts(name, value);
                }
                var instance = CreateInstance(requestedType, name, value);
                lock (_cacheLock)
                {
                    var cached = FindCandidate();
                    if (cached is not null)
                        return cached;
                    completeDefinition?.Invoke(instance);
                    RegisterInstance(instance);
                }
                return instance;
            }));
            TEnum result;
            try
            {
                result = slot.Value;
            }
            catch
            {
                _creationSlots.TryRemove(new KeyValuePair<string, Lazy<TEnum>>(value, slot));
                throw;
            }
            return Finish(result);

            TEnum? FindCandidate()
            {
                if (serializedToken is not null)
                    return FindSerialized(serializedToken);
                return _fromValue.TryGetValue(value, out var candidate) ? candidate : null;
            }

            TEnum Finish(TEnum instance)
            {
                lock (_cacheLock)
                {
                    if (serializedToken is not null)
                        instance = FindSerialized(serializedToken) ?? instance;
                    EnsureCompatible(instance, requestedType);
                    ThrowIfNameConflicts(name, instance.Value);
                    completeDefinition?.Invoke(instance);
                }
                return ReuseInstance(instance, requestedType, serializedToken is null ? name : null,
                    warnIncomplete: completeDefinition is null);
            }
        }

        private static void EnsureCompatible(TEnum instance, Type requestedType)
        {
            if (!requestedType.IsInstanceOfType(instance))
                throw new InvalidOperationException(
                    $"Enumeration value '{instance.Value}' already belongs to {instance.GetType().FullName} and cannot be used as {requestedType.FullName}.");
        }

        private static TEnum ReuseInstance(TEnum instance, Type requestedType, string? requestedName = null,
            bool warnIncomplete = true)
        {
            EnsureCompatible(instance, requestedType);
            var actualType = instance.GetType();
            if (actualType != requestedType)
                Logger?.LogWarning("Reusing enumeration value '{Value}' of type {ActualType} as {RequestedType}.",
                    instance.Value, actualType.FullName, requestedType.FullName);
            if (requestedName is not null && instance.Name != requestedName)
                Logger?.LogWarning("Enumeration value '{Value}' already exists with name '{Name}'; reusing it instead of name '{RequestedName}'.",
                    instance.Value, instance.Name, requestedName);
            if (warnIncomplete && instance is Enumeration<TEnum> enumeration)
                enumeration.WarnIfIncomplete();
            return instance;
        }

        private static void ThrowIfNameConflicts(string name, string value)
        {
            if (_fromName.TryGetValue(name, out var existing) && existing.Value != value)
                throw new InvalidOperationException($"Enumeration name '{name}' already exists with a different value '{existing.Value}'.");
        }

        // Called under _cacheLock, after the complete instance has been validated.
        private static void RegisterInstance(TEnum instance)
        {
            if (instance.GetType().GetEnumerationFamilyType() != typeof(Enumeration<TEnum>))
                throw new InvalidOperationException($"Type {instance.GetType().FullName} belongs to a different enumeration family.");
            if (_fromValue.TryGetValue(instance.Value, out var existing) && !ReferenceEquals(existing, instance))
                throw new InvalidOperationException(
                    $"Enumeration value '{instance.Value}' already has an instance of {existing.GetType().FullName}. Use FromValue or FromNameAndValue to reuse it instead of constructing another instance.");
            ThrowIfNameConflicts(instance.Name, instance.Value);
            _fromName[instance.Name] = instance;
            _fromNameIgnoreCase.TryAdd(instance.Name, instance);
            ValueCacheDictionary[instance.Name] = instance;
            // The global index is shared by all closed Enumeration<TEnum> families.
            lock (IEnumeration.ValueCacheDictionary)
            {
                IEnumeration.ValueCacheDictionary.TryAdd($"{typeof(TEnum).Name}.{instance.Name}", instance);
                IEnumeration.ValueCacheDictionary.TryAdd($"{instance.GetType().Name}.{instance.Name}", instance);
            }
            _fromValue[instance.Value] = instance;
        }

        private static TEnum CreateInstance(Type concreteType, string name, string value)
        {
            try
            {
                var constructor = _constructorCache.GetOrAdd(concreteType, CreateConstructor);
                var instance = constructor(name, value);
                if (instance is null || instance.GetType() != concreteType)
                {
                    throw new InvalidOperationException($"The enumeration constructor must produce an instance of {concreteType.FullName}.");
                }
                if (instance.Name != name || instance.Value != value)
                {
                    throw new InvalidOperationException("The enumeration constructor must preserve the requested name and value.");
                }

                return instance;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Cannot create enumeration with name '{name}' and value '{value}' of type {concreteType.FullName}. {exception.Message}",
                    exception);
            }
        }

        private static Func<string, string, TEnum> CreateConstructor(Type concreteType)
        {
            if (concreteType.IsAbstract || concreteType.IsInterface || !typeof(TEnum).IsAssignableFrom(concreteType))
                throw new InvalidOperationException($"Type {concreteType.FullName} cannot be constructed as an enumeration.");

            var constructor = concreteType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding,
                binder: null, types: new[] { typeof(string), typeof(string) }, modifiers: null);
            if (constructor is not null && constructor.GetParameters().All(parameter => !parameter.IsOptional))
            {
                var name = Expression.Parameter(typeof(string), "name");
                var value = Expression.Parameter(typeof(string), "value");
                return Expression.Lambda<Func<string, string, TEnum>>(
                    Expression.Convert(Expression.New(constructor, name, value), typeof(TEnum)), name, value).CompileFast();
            }

            if (!typeof(Enumeration<TEnum>).IsAssignableFrom(concreteType) ||
                concreteType.GetConstructor(Type.EmptyTypes) is null)
            {
                throw new InvalidOperationException(
                    $"Type {concreteType.FullName} requires an exact (string name, string value) constructor " +
                    "or a public parameterless constructor on its Enumeration family to enable dynamic creation.");
            }

            return (name, value) =>
            {
                var instance = (TEnum)concreteType.CreateInstanceFast();
                ((Enumeration<TEnum>)(object)instance).SetEnum(name, value);
                return instance;
            };
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
            return type.GetEnumerationFamilyType().GenericTypeArguments[0];
        }

        internal static Type GetEnumerationFamilyType(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (!typeof(IEnumeration).IsAssignableFrom(type))
                throw new ArgumentException("The requested type must implement IEnumeration.", nameof(type));
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Enumeration<>))
                    return current;
            }
            return typeof(Enumeration<>).MakeGenericType(type);
        }

    }
}
