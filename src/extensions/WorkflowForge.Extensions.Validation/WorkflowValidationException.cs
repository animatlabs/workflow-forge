using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Exception thrown when workflow validation fails.
    /// </summary>
    public sealed class WorkflowValidationException : Exception
    {
        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowValidationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errors">The validation errors.</param>
        public WorkflowValidationException(string message, IEnumerable<ValidationError> errors)
            : base(message)
        {
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowValidationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="errors">The validation errors.</param>
        public WorkflowValidationException(string message, Exception innerException, IEnumerable<ValidationError> errors)
            : base(message, innerException)
        {
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }
    }
}