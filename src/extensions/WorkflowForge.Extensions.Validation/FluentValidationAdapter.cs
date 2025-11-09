using FluentValidation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Adapter that bridges FluentValidation validators to WorkflowForge validation system.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    public sealed class FluentValidationAdapter<T> : IWorkflowValidator<T>
    {
        private readonly IValidator<T> _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationAdapter{T}"/> class.
        /// </summary>
        /// <param name="validator">The FluentValidation validator.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when validator is null.</exception>
        public FluentValidationAdapter(IValidator<T> validator)
        {
            _validator = validator ?? throw new System.ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Validates the specified data using the FluentValidation validator.
        /// </summary>
        /// <param name="data">The data to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the validation result.</returns>
        public async Task<ValidationResult> ValidateAsync(T data, CancellationToken cancellationToken = default)
        {
            var result = await _validator.ValidateAsync(data, cancellationToken);

            if (result.IsValid)
            {
                return ValidationResult.Success;
            }

            var errors = result.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToArray();

            return ValidationResult.Failure(errors);
        }
    }
}