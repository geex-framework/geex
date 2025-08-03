using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Geex.Validation
{
    public class ValidateRule<T> : ValidateRule
    {
        public Func<T, bool> Predicate { get; }

        protected internal ValidateRule(string ruleKey, Func<T, bool> predicate)
        {
            RuleKey = ruleKey;
            Predicate = predicate;
        }

        public static ValidateRule<T> Create(Func<T, bool> predicate,
            string validatorName,
            params object[] parameters)
        {
            var ruleKey = GenerateRuleKey(validatorName, parameters);
            return (ValidateRule<T>)_cache.GetOrAdd(ruleKey, _ => new ValidateRule<T>(ruleKey, predicate));
        }

        private static string GenerateRuleKey(string validatorName, params object[] parameters)
        {
            if (parameters?.Length == 0)
                return validatorName;

            var capacity = validatorName.Length + 1; // "?"
            if (parameters != null)
            {
                capacity += parameters.Length * 10; // Rough estimate
            }

            var sb = new System.Text.StringBuilder(capacity);
            sb.Append(validatorName);

            if (parameters?.Length > 0)
            {
                sb.Append('?');
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) sb.Append('&');
                    sb.Append(parameters[i].ToJsonSafe());
                }
            }
            return sb.ToString();
        }

        public ValidationResult Validate(T value)
        {
            try
            {
                var isValid = Predicate(value);
                if (isValid)
                {
                    return ValidationResult.Success;
                }

                // Create more informative error message
                var valueString = value?.ToString() ?? "null";
                if (valueString.Length > 50)
                {
                    valueString = valueString.Substring(0, 47) + "...";
                }

                return new ValidationResult($"Validation failed for rule '{RuleKey}' with value '{valueString}'.", new[] { RuleKey });
            }
            catch (Exception ex)
            {
                return new ValidationResult($"Validation error in rule '{RuleKey}': {ex.Message}", new[] { RuleKey });
            }
        }

        public static ValidateRule<T> operator &(ValidateRule<T> first, ValidateRule<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            // Use a more structured approach with parentheses
            var ruleKey = $"And({first.RuleKey},{second.RuleKey})";
            return (ValidateRule<T>)_cache.GetOrAdd(ruleKey, _ => new ValidateRule<T>(ruleKey, value => first.Predicate(value) && second.Predicate(value)));
        }

        public static ValidateRule<T> operator |(ValidateRule<T> first, ValidateRule<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            // Use a more structured approach with parentheses
            var ruleKey = $"Or({first.RuleKey},{second.RuleKey})";
            return (ValidateRule<T>)_cache.GetOrAdd(ruleKey, _ => new ValidateRule<T>(ruleKey, value => first.Predicate(value) || second.Predicate(value)));
        }

        /// <inheritdoc />
        public override ValidationResult Validate(object value)
        {
            return this.Validate((T)value);
        }
    }

    public abstract partial class ValidateRule
    {
        protected static readonly ConcurrentDictionary<string, ValidateRule> _cache = new();
        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();
        private static readonly ConcurrentDictionary<string, MethodInfo> _methodCache = new();

        /// <inheritdoc />
        public override string ToString() => $"{nameof(ValidateRule)}/{this.RuleKey}";

        public string RuleKey { get; protected set; }
        protected static ValidateRule<T> CreateRule<T>(Func<T, bool> predicate, object[] parameters = default, [CallerMemberName] string callMemberName = default)
        {
            return ValidateRule<T>.Create(predicate, callMemberName, parameters: parameters);
        }

        public static ValidateRule Null = new ValidateRule<object>(string.Empty, _ => true);

        public abstract ValidationResult Validate(object value);

        internal static ValidateRule FromRuleKey(string ruleKey)
        {
            if (_cache.TryGetValue(ruleKey, out var existed))
            {
                return existed;
            }

            return ReconstructRuleFromKey(ruleKey);
        }

        private static ValidateRule ReconstructRuleFromKey(string ruleKey)
        {
            try
            {
                // Handle composite rules (And/Or operations)
                if (ruleKey.StartsWith("And(") || ruleKey.StartsWith("Or("))
                {
                    return ReconstructCompositeRule(ruleKey);
                }

                // Parse the rule key: validatorName?param1&param2&...
                var parts = ruleKey.Split('?', 2);
                var validatorName = parts[0];
                var parameters = new object[0];

                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    var paramStrings = parts[1].Split('&');
                    parameters = paramStrings.Select(DeserializeParameter).ToArray();
                }

                // Find and invoke the corresponding static method
                var method = _methodCache.GetOrAdd(validatorName, methodName =>
                    typeof(ValidateRule).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static));

                if (method != null)
                {
                    var rule = method.Invoke(null, parameters) as ValidateRule;
                    if (rule != null)
                    {
                        _cache.TryAdd(ruleKey, rule);
                        return rule;
                    }
                }

                // If we can't reconstruct the rule, return a null rule
                return Null;
            }
            catch
            {
                // Fallback to null rule on any error
                return Null;
            }
        }

        private static ValidateRule ReconstructCompositeRule(string ruleKey)
        {
            try
            {
                bool isAnd = ruleKey.StartsWith("And(");
                int prefixLength = isAnd ? 4 : 3; // "And(" or "Or("

                // Ensure the rule key ends with ")"
                if (!ruleKey.EndsWith(")"))
                    return Null;

                // Extract the content between the parentheses
                var content = ruleKey.Substring(prefixLength, ruleKey.Length - prefixLength - 1);
                var (firstKey, secondKey) = SplitCompositeRuleKey(content);

                if (!string.IsNullOrEmpty(firstKey) && !string.IsNullOrEmpty(secondKey))
                {
                    var firstRule = FromRuleKey(firstKey);
                    var secondRule = FromRuleKey(secondKey);

                    // Create a dynamic composite rule
                    return CreateDynamicCompositeRule(ruleKey, firstRule, secondRule, isAnd);
                }

                return Null;
            }
            catch
            {
                return Null;
            }
        }

        private static (string firstKey, string secondKey) SplitCompositeRuleKey(string content)
        {
            // Split a string of format "ruleKey1,ruleKey2" handling nested parentheses correctly
            int nestingLevel = 0;
            int commaIndex = -1;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '(')
                {
                    nestingLevel++;
                }
                else if (c == ')')
                {
                    nestingLevel--;
                }
                else if (c == ',' && nestingLevel == 0)
                {
                    commaIndex = i;
                    break;
                }
            }

            if (commaIndex > 0)
            {
                return (content.Substring(0, commaIndex), content.Substring(commaIndex + 1));
            }

            return (string.Empty, string.Empty);
        }

        private static ValidateRule CreateDynamicCompositeRule(string ruleKey, ValidateRule first, ValidateRule second, bool isAnd)
        {
            // Create a generic composite rule that can handle any type
            var rule = new DynamicCompositeRule(ruleKey, first, second, isAnd);
            _cache.TryAdd(ruleKey, rule);
            return rule;
        }

        private static object DeserializeParameter(string paramJson)
        {
            try
            {
                // Handle null or empty
                if (string.IsNullOrEmpty(paramJson) || paramJson == "null")
                    return null;

                // Remove surrounding quotes if present
                var trimmed = paramJson.Trim();
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    trimmed = trimmed.Substring(1, trimmed.Length - 2);
                }

                // Try to parse as different types
                if (int.TryParse(trimmed, out var intValue))
                    return intValue;

                if (decimal.TryParse(trimmed, out var decimalValue))
                    return decimalValue;

                if (double.TryParse(trimmed, out var doubleValue))
                    return doubleValue;

                if (bool.TryParse(trimmed, out var boolValue))
                    return boolValue;

                // Try to deserialize as JSON object using the extension method
                try
                {
                    var objectValue = paramJson.ToObjectSafe<object>();
                    if (objectValue != null)
                        return objectValue;
                }
                catch
                {
                    // Fall through to string
                }

                // Default to string
                return trimmed;
            }
            catch
            {
                // Fallback to the original string
                return paramJson;
            }
        }
    }

    internal class DynamicCompositeRule : ValidateRule
    {
        private readonly ValidateRule _first;
        private readonly ValidateRule _second;
        private readonly bool _isAnd;

        public DynamicCompositeRule(string ruleKey, ValidateRule first, ValidateRule second, bool isAnd)
        {
            RuleKey = ruleKey;
            _first = first;
            _second = second;
            _isAnd = isAnd;
        }

        public override ValidationResult Validate(object value)
        {
            var firstResult = _first.Validate(value);
            var secondResult = _second.Validate(value);

            if (_isAnd)
            {
                // Both must succeed
                if (firstResult == ValidationResult.Success && secondResult == ValidationResult.Success)
                {
                    return ValidationResult.Success;
                }

                // Return the first error or combine both errors
                var errors = new List<string>();
                if (firstResult != ValidationResult.Success)
                    errors.AddRange(firstResult.MemberNames);
                if (secondResult != ValidationResult.Success)
                    errors.AddRange(secondResult.MemberNames);

                return new ValidationResult($"Validation failed for composite rule '{RuleKey}' with value '{value}'.", errors);
            }
            else
            {
                // At least one must succeed
                if (firstResult == ValidationResult.Success || secondResult == ValidationResult.Success)
                {
                    return ValidationResult.Success;
                }

                // Both failed
                var errors = new List<string>();
                if (firstResult != ValidationResult.Success)
                    errors.AddRange(firstResult.MemberNames);
                if (secondResult != ValidationResult.Success)
                    errors.AddRange(secondResult.MemberNames);

                return new ValidationResult($"Validation failed for composite rule '{RuleKey}' with value '{value}'.", errors);
            }
        }
    }
}
