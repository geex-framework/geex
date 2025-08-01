using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            
            var ruleKey = $"And_{first.RuleKey}_{second.RuleKey}";
            return (ValidateRule<T>)_cache.GetOrAdd(ruleKey, _ => new ValidateRule<T>(ruleKey, value => first.Predicate(value) && second.Predicate(value)));
        }

        public static ValidateRule<T> operator |(ValidateRule<T> first, ValidateRule<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            
            var ruleKey = $"Or_{first.RuleKey}_{second.RuleKey}";
            return (ValidateRule<T>)_cache.GetOrAdd(ruleKey, _ => new ValidateRule<T>(ruleKey, value => first.Predicate(value) || second.Predicate(value)));
        }

        /// <inheritdoc />
        public override ValidationResult Validate(object value)
        {
            return this.Validate((T)value);
        }
    }

    public abstract class ValidateRule
    {
        protected static readonly ConcurrentDictionary<string, ValidateRule> _cache = new();
        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();
        private static readonly ConcurrentDictionary<string, MethodInfo> _methodCache = new();
        
        public string RuleKey { get; protected set; }
        protected static ValidateRule<T> CreateRule<T>(Func<T, bool> predicate, object[] parameters = default, [CallerMemberName] string callMemberName = default)
        {
            return ValidateRule<T>.Create(predicate, callMemberName, parameters: parameters);
        }

        // String validation
        public static ValidateRule<string> Regex(string pattern)
        {
            return CreateRule<string>(value => 
            {
                if (string.IsNullOrEmpty(value)) return false;
                var regex = _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
                return regex.IsMatch(value);
            }, [pattern]);
        }

        public static ValidateRule<string> LengthMin(int minLength)
        {
            if (minLength < 0) throw new ArgumentOutOfRangeException(nameof(minLength), "Minimum length cannot be negative");
            return CreateRule<string>(value => (value?.Length ?? 0) >= minLength, [minLength]);
        }

        public static ValidateRule<string> LengthMax(int maxLength)
        {
            if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length cannot be negative");
            return CreateRule<string>(value => (value?.Length ?? 0) <= maxLength, [maxLength]);
        }

        public static ValidateRule<string> LengthRange(int minLength, int maxLength)
        {
            if (minLength < 0) throw new ArgumentOutOfRangeException(nameof(minLength), "Minimum length cannot be negative");
            if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length cannot be negative");
            if (minLength > maxLength) throw new ArgumentException("Minimum length cannot be greater than maximum length");
            
            return CreateRule<string>(value =>
            {
                var length = value?.Length ?? 0;
                return length >= minLength && length <= maxLength;
            }, [minLength, maxLength]);
        }

        // Numeric validation
        public static ValidateRule<T> Min<T>(T minValue) where T : IComparable<T>
        {
            return CreateRule<T>(value => value.CompareTo(minValue) >= 0, [minValue]);
        }

        public static ValidateRule<T> Max<T>(T maxValue) where T : IComparable<T>
        {
            return CreateRule<T>(value => value.CompareTo(maxValue) <= 0, [maxValue]);
        }

        public static ValidateRule<T> Range<T>(T minValue, T maxValue) where T : IComparable<T>
        {
            return CreateRule<T>(value =>
                value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0, [minValue, maxValue]);
        }

        // Email validation
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        public static ValidateRule<string> Email()
        {
            return CreateRule<string>(value =>
                !string.IsNullOrEmpty(value) &&
                EmailRegex.IsMatch(value));
        }

        private static readonly HashSet<string> DisposableDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "10minutemail.com", "tempmail.org", "guerrillamail.com", "mailinator.com", 
            "yopmail.com", "throwaway.email", "temp-mail.org"
        };
        
        public static ValidateRule<string> NotDisposableEmail()
        {
            return CreateRule<string>(value =>
            {
                if (string.IsNullOrEmpty(value)) return false;
                var atIndex = value.LastIndexOf('@');
                if (atIndex <= 0 || atIndex == value.Length - 1) return false;
                var domain = value.Substring(atIndex + 1);
                return !DisposableDomains.Contains(domain);
            });
        }

        // Phone validation
        private static readonly Regex ChinesePhoneRegex = new Regex(@"^1[3-9]\d{9}$", RegexOptions.Compiled);
        public static ValidateRule<string> ChinesePhone()
        {
            return CreateRule<string>(value =>
                !string.IsNullOrEmpty(value) &&
                ChinesePhoneRegex.IsMatch(value));
        }

        // Business validation
        public static ValidateRule<decimal> Price()
        {
            return CreateRule<decimal>(value => value >= 0 && value <= 999999.99m);
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
                if (ruleKey.StartsWith("And_") || ruleKey.StartsWith("Or_"))
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
                if (ruleKey.StartsWith("And_"))
                {
                    var content = ruleKey.Substring(4);
                    var (firstKey, secondKey) = SplitCompositeRuleKey(content);

                    if (!string.IsNullOrEmpty(firstKey) && !string.IsNullOrEmpty(secondKey))
                    {
                        var firstRule = FromRuleKey(firstKey);
                        var secondRule = FromRuleKey(secondKey);

                        // Create a dynamic And rule
                        return CreateDynamicCompositeRule(ruleKey, firstRule, secondRule, true);
                    }
                }
                else if (ruleKey.StartsWith("Or_"))
                {
                    var content = ruleKey.Substring(3);
                    var (firstKey, secondKey) = SplitCompositeRuleKey(content);

                    if (!string.IsNullOrEmpty(firstKey) && !string.IsNullOrEmpty(secondKey))
                    {
                        var firstRule = FromRuleKey(firstKey);
                        var secondRule = FromRuleKey(secondKey);

                        // Create a dynamic Or rule
                        return CreateDynamicCompositeRule(ruleKey, firstRule, secondRule, false);
                    }
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
            // Find the split point by looking for the pattern that separates two rule keys
            // This is more complex than simple splitting by "_" because rule keys themselves may contain "_"

            // Strategy: Try to find known validator names at the beginning
            var validatorNames = new[] { "Regex", "LengthMin", "LengthMax", "LengthRange", "Min", "Max", "Range", "Email", "NotDisposableEmail", "ChinesePhone", "Price", "And_", "Or_" };

            foreach (var validatorName in validatorNames)
            {
                var searchPattern = "_" + validatorName;
                var index = content.IndexOf(searchPattern);
                if (index > 0)
                {
                    var firstKey = content.Substring(0, index);
                    var secondKey = content.Substring(index + 1);
                    return (firstKey, secondKey);
                }
            }

            // Fallback: simple split (may not work for all cases)
            var parts = content.Split(new[] { '_' }, 2);
            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
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
