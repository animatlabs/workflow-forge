using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Defines validation logic for workflow operations.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    public interface IWorkflowValidator<in T>
    {
        /// <summary>
        /// Validates the specified data asynchronously.
        /// </summary>
        /// <param name="data">The data to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the validation result.</returns>
        Task<ValidationResult> ValidateAsync(T data, CancellationToken cancellationToken = default);
    }
}