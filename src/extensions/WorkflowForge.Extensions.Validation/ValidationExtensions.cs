using FluentValidation;
using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Extension methods for adding validation capabilities to WorkflowForge.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Adds a FluentValidation validator to the foundry's middleware pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="validator">The FluentValidation validator.</param>
        /// <param name="dataExtractor">Function to extract data from foundry properties for validation.</param>
        /// <param name="throwOnFailure">If true, throws exception on validation failure; otherwise logs and continues.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public static IWorkflowFoundry AddValidation<T>(
            this IWorkflowFoundry foundry,
            IValidator<T> validator,
            Func<IWorkflowFoundry, T?> dataExtractor,
            bool throwOnFailure = true) where T : class
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (dataExtractor == null) throw new ArgumentNullException(nameof(dataExtractor));

            var adapter = new FluentValidationAdapter<T>(validator);

            var middleware = new ValidationMiddleware(
                foundry.Logger,
                new WorkflowValidatorAdapter<T>(adapter),
                f => dataExtractor(f),
                throwOnFailure);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds a custom workflow validator to the foundry's middleware pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="validator">The workflow validator.</param>
        /// <param name="dataExtractor">Function to extract data from foundry properties for validation.</param>
        /// <param name="throwOnFailure">If true, throws exception on validation failure; otherwise logs and continues.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public static IWorkflowFoundry AddValidation<T>(
            this IWorkflowFoundry foundry,
            IWorkflowValidator<T> validator,
            Func<IWorkflowFoundry, T?> dataExtractor,
            bool throwOnFailure = true) where T : class
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (dataExtractor == null) throw new ArgumentNullException(nameof(dataExtractor));

            var middleware = new ValidationMiddleware(
                foundry.Logger,
                new WorkflowValidatorAdapter<T>(validator),
                f => dataExtractor(f),
                throwOnFailure);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Validates data using a FluentValidation validator and stores the result in foundry properties.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="validator">The FluentValidation validator.</param>
        /// <param name="data">The data to validate.</param>
        /// <param name="propertyKey">The foundry property key to store the validation result.</param>
        /// <returns>A task representing the validation result.</returns>
        public static async System.Threading.Tasks.Task<ValidationResult> ValidateAsync<T>(
            this IWorkflowFoundry foundry,
            IValidator<T> validator,
            T data,
            string propertyKey = "ValidationResult")
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (validator == null) throw new ArgumentNullException(nameof(validator));

            var adapter = new FluentValidationAdapter<T>(validator);
            var result = await adapter.ValidateAsync(data);

            foundry.Properties[propertyKey] = result;
            foundry.Properties[$"{propertyKey}.IsValid"] = result.IsValid;

            if (!result.IsValid)
            {
                foundry.Properties[$"{propertyKey}.Errors"] = result.Errors;
            }

            return result;
        }
    }

    internal sealed class WorkflowValidatorAdapter<T> : IWorkflowValidator<object>
    {
        private readonly IWorkflowValidator<T> _validator;

        public WorkflowValidatorAdapter(IWorkflowValidator<T> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async System.Threading.Tasks.Task<ValidationResult> ValidateAsync(object data, System.Threading.CancellationToken cancellationToken = default)
        {
            if (data is T typedData)
            {
                return await _validator.ValidateAsync(typedData, cancellationToken);
            }

            return ValidationResult.Failure($"Data type mismatch. Expected {typeof(T).Name}, got {data?.GetType().Name ?? "null"}");
        }
    }
}