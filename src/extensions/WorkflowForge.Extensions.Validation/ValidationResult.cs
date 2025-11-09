using System.Collections.Generic;
using System.Linq;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public sealed class ValidationResult
    {
        private static readonly ValidationResult _success = new ValidationResult(true, new List<ValidationError>());

        /// <summary>
        /// Gets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Gets a successful validation result.
        /// </summary>
        public static ValidationResult Success => _success;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Indicates whether validation was successful.</param>
        /// <param name="errors">The validation errors.</param>
        public ValidationResult(bool isValid, IEnumerable<ValidationError> errors)
        {
            IsValid = isValid;
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Creates a failed validation result with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationResult Failure(params ValidationError[] errors)
        {
            return new ValidationResult(false, errors);
        }

        /// <summary>
        /// Creates a failed validation result with a single error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(false, new[] { new ValidationError(string.Empty, errorMessage) });
        }
    }
}