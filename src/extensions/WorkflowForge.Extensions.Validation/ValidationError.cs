namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Represents a validation error with property name and error message.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// Gets the name of the property that failed validation.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The error message.</param>
        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        /// <summary>
        /// Returns a string representation of the validation error.
        /// </summary>
        /// <returns>A formatted error message.</returns>
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(PropertyName)
                ? ErrorMessage
                : $"{PropertyName}: {ErrorMessage}";
        }
    }
}