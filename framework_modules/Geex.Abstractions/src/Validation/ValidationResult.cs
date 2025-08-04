using System;
using System.Collections.Generic;
using System.Linq;

namespace Geex.Validation
{
    public struct ValidationResult:IEquatable<ValidationResult>
    {
        /// <summary>Represents the success of the validation.</summary>
        public static readonly ValidationResult Success = new(null, []);

        /// <summary>Initializes a new instance of the <see cref="ValidationResult" /> by using an error message.</summary>
        /// <param name="errorMessage">The error message.</param>
        public ValidationResult(string errorMessage)
          : this(errorMessage, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ValidationResult" /> by using an error message and a list of members that have validation errors.</summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="memberNames">The list of member names that have validation errors.</param>
        public ValidationResult(string? errorMessage, IEnumerable<string>? memberNames)
        {
            this.ErrorMessage = errorMessage;
            this.MemberNames = (IEnumerable<string>)((object)memberNames ?? Array.Empty<string>());
        }

        /// <summary>Gets the collection of member names that indicate which fields have validation errors.</summary>
        /// <returns>The collection of member names that indicate which fields have validation errors.</returns>
        public IEnumerable<string> MemberNames { get; }

        /// <summary>Gets the error message for the validation.</summary>
        /// <returns>The error message for the validation.</returns>
        public string? ErrorMessage { get; set; }

        /// <summary>Returns a string representation of the current validation result.</summary>
        /// <returns>The current validation result.</returns>
        public override string ToString() => this.ErrorMessage ?? base.ToString();

        /// <inheritdoc />
        public bool Equals(ValidationResult other)
        {
            // Fast path: reference equality (same instance)
            if (ReferenceEquals(this, other))
                return true;

            // Fast path: both are Success
            if (string.IsNullOrEmpty(ErrorMessage) && string.IsNullOrEmpty(other.ErrorMessage) &&
                !MemberNames.Any() && !other.MemberNames.Any())
                return true;

            // Fast path: different error messages
            if (!string.Equals(ErrorMessage, other.ErrorMessage))
                return false;

            // Expensive comparison last
            return MemberNames.SequenceEqual(other.MemberNames);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ValidationResult other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(MemberNames, ErrorMessage);
        }

        public static bool operator ==(ValidationResult left, ValidationResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValidationResult left, ValidationResult right)
        {
            return !left.Equals(right);
        }
    }
}
