using Microsoft.Extensions.Options;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.DependencyInjection
{
    /// <summary>
    /// ASP.NET Core options validator for <see cref="WorkflowForgeOptions"/>.
    /// Integrates with the IOptions validation pipeline for startup-time validation.
    /// </summary>
    /// <remarks>
    /// This validator is automatically registered when using AddWorkflowForge and ensures
    /// configuration errors are caught at application startup rather than at runtime.
    /// </remarks>
    public class WorkflowForgeOptionsValidator : IValidateOptions<WorkflowForgeOptions>
    {
        /// <summary>
        /// Validates the specified <see cref="WorkflowForgeOptions"/> instance.
        /// </summary>
        /// <param name="name">The name of the options instance being validated (can be null for default).</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure with error messages.</returns>
        public ValidateOptionsResult Validate(string? name, WorkflowForgeOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("WorkflowForgeOptions cannot be null");
            }

            var errors = options.Validate();
            return errors.Count > 0
                ? ValidateOptionsResult.Fail(errors)
                : ValidateOptionsResult.Success;
        }
    }
}
