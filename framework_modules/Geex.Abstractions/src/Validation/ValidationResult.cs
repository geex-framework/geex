using System;
using System.Collections.Generic;

namespace Geex.Validation
{
    public class ValidationResult
    {
        /// <summary>Represents the success of the validation.</summary>
        public static readonly ValidationResult? Success = new ();
        private ValidationResult()
        {
            this.ErrorMessage = null;
            this.MemberNames = [];
        }
        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> class by using an error message.</summary>
        /// <param name="errorMessage">The error message.</param>
        public ValidationResult(string? errorMessage)
          : this(errorMessage, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> class by using an error message and a list of members that have validation errors.</summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="memberNames">The list of member names that have validation errors.</param>
        public ValidationResult(string? errorMessage, IEnumerable<string>? memberNames)
        {
            this.ErrorMessage = errorMessage;
            this.MemberNames = (IEnumerable<string>)((object)memberNames ?? Array.Empty<string>());
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> class by using a <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> object.</summary>
        /// <param name="validationResult">The validation result object.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="validationResult" /> is <see langword="null" />.</exception>
        protected ValidationResult(ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult, nameof(validationResult));
            this.ErrorMessage = validationResult.ErrorMessage;
            this.MemberNames = validationResult.MemberNames;
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
    }
}
