using System;
using System.Linq;

namespace Geex.Validation
{
    /// <summary>
    /// Attribute for applying validation rules to properties, parameters, or fields
    /// This attribute only stores validation metadata and does not execute validation logic
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = true)]
    public class ValidateAttribute : Attribute
    {
        public string RuleKey { get; }
        public string Message { get; }

        // Constructor for rule key based validation
        public ValidateAttribute(string ruleKey, object[] parameters, string message = null)
        {
            ruleKey += parameters is { Length: > 0 }
                ? "?" + string.Join("&", parameters.Select(p => p.ToJsonSafe()))
                : "";
            RuleKey = ruleKey ?? throw new ArgumentNullException(nameof(ruleKey));
            Message = message;
        }

        // Constructor for rule key based validation
        public ValidateAttribute(string ruleKey, string message = null)
        {
            RuleKey = ruleKey ?? throw new ArgumentNullException(nameof(ruleKey));
            Message = message;
        }

        /// <summary>
        /// Convert this attribute to a ValidateDirective
        /// </summary>
        /// <returns>ValidateDirective instance</returns>
        public ValidateDirective ToDirective()
        {
            return new ValidateDirective(RuleKey, Message);
        }
    }

    /// <summary>
    /// Provides compile-time constants for common validation rule keys
    /// </summary>
    public static class ValidateRuleKeys
    {
        public const string Email = nameof(ValidateRule.Email);
        public const string ChinesePhone = nameof(ValidateRule.ChinesePhone);
        public const string NotDisposableEmail = nameof(ValidateRule.NotDisposableEmail);
        public const string Price = nameof(ValidateRule.Price);
        public const string LengthMin = nameof(ValidateRule.LengthMin);
        public const string LengthMax = nameof(ValidateRule.LengthMax);
        public const string LengthRange = nameof(ValidateRule.LengthRange);
        public const string Min = nameof(ValidateRule.Min);
        public const string Max = nameof(ValidateRule.Max);
        public const string Range = nameof(ValidateRule.Range);
        public const string Regex = nameof(ValidateRule.Regex);
    }
}
