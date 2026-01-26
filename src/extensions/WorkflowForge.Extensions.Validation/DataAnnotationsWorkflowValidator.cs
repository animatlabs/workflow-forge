using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Validates data using System.ComponentModel.DataAnnotations attributes.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    public sealed class DataAnnotationsWorkflowValidator<T> : IWorkflowValidator<T>
    {
        /// <summary>
        /// Validates the specified data using DataAnnotations.
        /// </summary>
        /// <param name="data">The data to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the validation result.</returns>
        public Task<ValidationResult> ValidateAsync(T data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                return Task.FromResult(ValidationResult.Failure("Validation data cannot be null."));
            }

            var context = new ValidationContext(data);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(data, context, results, validateAllProperties: true);

            if (isValid)
            {
                return Task.FromResult(ValidationResult.Success);
            }

            var errors = results
                .SelectMany(result =>
                {
                    var message = result.ErrorMessage ?? "Validation failed.";
                    if (result.MemberNames != null && result.MemberNames.Any())
                    {
                        return result.MemberNames.Select(member => new ValidationError(member, message));
                    }

                    return new[] { new ValidationError("Object", message) };
                })
                .ToArray();

            return Task.FromResult(ValidationResult.Failure(errors));
        }
    }
}