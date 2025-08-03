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
}
