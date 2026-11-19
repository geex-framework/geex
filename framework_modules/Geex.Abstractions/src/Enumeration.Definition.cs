using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Geex;

/// <summary>
/// A consistent snapshot of additional enumeration details and their completion state.
/// Details must be a value or immutable data; reference values are not cloned.
/// </summary>
public sealed record EnumerationDefinition<TDetails>(TDetails Details, bool IsComplete);

/// <summary>
/// An opt-in enumeration whose external details can be supplied after its identity is known.
/// </summary>
public abstract class Enumeration<TEnum, TDetails> : Enumeration<TEnum>
    where TEnum : class, IEnumeration
{
    private EnumerationDefinition<TDetails> _definition;
    private int _incompleteWarningReported;

    /// <summary>Provides explicit defaults for a parameterless dynamic construction path.</summary>
    protected Enumeration(TDetails unknownDetails)
    {
        ArgumentNullException.ThrowIfNull(unknownDetails);
        _definition = new(unknownDetails, false);
    }

    /// <summary>Constructs an identity with explicitly complete or incomplete details.</summary>
    protected Enumeration(string name, string value, TDetails details, bool isComplete) : base(name, value)
    {
        ArgumentNullException.ThrowIfNull(details);
        _definition = new(details, isComplete);
    }

    /// <summary>Gets one atomic snapshot. Retain it when reading multiple related properties.</summary>
    public EnumerationDefinition<TDetails> Definition => Volatile.Read(ref _definition);

    /// <summary>Creates a complete member or completes the existing instance without changing its identity.</summary>
    public static TEnum Define(string name, string value, TDetails details) =>
        DefineInstance(typeof(TEnum), name, value, details);

    /// <summary>Defines a compatible requested subtype in the same enumeration family.</summary>
    public static TChild Define<TChild>(string name, string value, TDetails details)
        where TChild : class, TEnum =>
        (TChild)DefineInstance(typeof(TChild), name, value, details);

    internal void CompleteDefinition(TDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var completed = new EnumerationDefinition<TDetails>(details, true);
        while (true)
        {
            var current = Definition;
            if (current.IsComplete)
            {
                if (!EqualityComparer<TDetails>.Default.Equals(current.Details, details))
                    throw new InvalidOperationException($"Enumeration value '{Value}' already has a different complete definition.");
                return;
            }
            if (ReferenceEquals(Interlocked.CompareExchange(ref _definition, completed, current), current))
                return;
        }
    }

    internal override void WarnIfIncomplete()
    {
        if (Definition.IsComplete)
            return;
        var logger = Logger;
        if (logger is not null && Interlocked.CompareExchange(ref _incompleteWarningReported, 1, 0) == 0)
            logger.LogWarning("Enumeration value '{Value}' of type {EnumerationType} has incomplete details; returning its declared defaults.",
                Value, GetType().FullName);
    }
}
