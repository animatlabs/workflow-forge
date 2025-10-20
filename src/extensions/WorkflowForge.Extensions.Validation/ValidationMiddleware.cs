using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Middleware that validates workflow data using registered validators.
    /// </summary>
    public sealed class ValidationMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowForgeLogger _logger;
        private readonly Func<IWorkflowFoundry, object?> _dataExtractor;
        private readonly IWorkflowValidator<object> _validator;
        private readonly bool _throwOnFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger for validation events.</param>
        /// <param name="validator">The validator to use.</param>
        /// <param name="dataExtractor">Function to extract data from foundry for validation.</param>
        /// <param name="throwOnFailure">If true, throws exception on validation failure; otherwise logs and continues.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public ValidationMiddleware(
            IWorkflowForgeLogger logger,
            IWorkflowValidator<object> validator,
            Func<IWorkflowFoundry, object?> dataExtractor,
            bool throwOnFailure = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _dataExtractor = dataExtractor ?? throw new ArgumentNullException(nameof(dataExtractor));
            _throwOnFailure = throwOnFailure;
        }

        /// <summary>
        /// Executes validation before delegating to the next middleware.
        /// </summary>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="inputData">Optional input data.</param>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation result.</returns>
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var dataToValidate = _dataExtractor(foundry);

            if (dataToValidate == null)
            {
                _logger.LogWarning($"Validation skipped for operation '{operation.Name}': no data to validate");
                return await next();
            }

            var validationResult = await _validator.ValidateAsync(dataToValidate, cancellationToken);

            if (validationResult.IsValid)
            {
                _logger.LogInformation($"Validation passed for operation '{operation.Name}'");
                foundry.Properties[$"Validation.{operation.Name}.Status"] = "Success";
                return await next();
            }

            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ToString()));
            _logger.LogError($"Validation failed for operation '{operation.Name}': {errorMessages}");

            foundry.Properties[$"Validation.{operation.Name}.Status"] = "Failed";
            foundry.Properties[$"Validation.{operation.Name}.Errors"] = validationResult.Errors;

            if (_throwOnFailure)
            {
                throw new WorkflowValidationException(
                    $"Validation failed for operation '{operation.Name}': {errorMessages}",
                    validationResult.Errors);
            }

            return await next();
        }
    }
}