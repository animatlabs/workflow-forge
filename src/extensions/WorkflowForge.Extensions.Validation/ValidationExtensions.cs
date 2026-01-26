using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Validation.Options;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Extension methods for adding validation capabilities to WorkflowForge.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Adds a DataAnnotations validator to the foundry's middleware pipeline with options.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="dataExtractor">Function to extract data from foundry properties for validation.</param>
        /// <param name="options">Configuration options for validation behavior.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public static IWorkflowFoundry UseValidation<T>(
            this IWorkflowFoundry foundry,
            Func<IWorkflowFoundry, T?> dataExtractor,
            ValidationMiddlewareOptions? options = null) where T : class
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (dataExtractor == null) throw new ArgumentNullException(nameof(dataExtractor));

            options ??= new ValidationMiddlewareOptions();

            if (!options.Enabled)
            {
                foundry.Logger.LogInformation("Validation middleware is disabled via configuration");
                return foundry;
            }

            var middleware = new ValidationMiddleware(
                foundry.Logger,
                new WorkflowValidatorAdapter<T>(new DataAnnotationsWorkflowValidator<T>()),
                f => dataExtractor(f),
                options);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds a custom workflow validator to the foundry's middleware pipeline with options.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="validator">The workflow validator.</param>
        /// <param name="dataExtractor">Function to extract data from foundry properties for validation.</param>
        /// <param name="options">Configuration options for validation behavior.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public static IWorkflowFoundry UseValidation<T>(
            this IWorkflowFoundry foundry,
            IWorkflowValidator<T> validator,
            Func<IWorkflowFoundry, T?> dataExtractor,
            ValidationMiddlewareOptions? options = null) where T : class
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (dataExtractor == null) throw new ArgumentNullException(nameof(dataExtractor));

            options ??= new ValidationMiddlewareOptions();

            if (!options.Enabled)
            {
                foundry.Logger.LogInformation("Validation middleware is disabled via configuration");
                return foundry;
            }

            var middleware = new ValidationMiddleware(
                foundry.Logger,
                new WorkflowValidatorAdapter<T>(validator),
                f => dataExtractor(f),
                options);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Validates data using DataAnnotations and stores the result in foundry properties.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="data">The data to validate.</param>
        /// <param name="propertyKey">The foundry property key to store the validation result.</param>
        /// <returns>A task representing the validation result.</returns>
        public static async System.Threading.Tasks.Task<ValidationResult> ValidateAsync<T>(
            this IWorkflowFoundry foundry,
            T data,
            string propertyKey = "ValidationResult")
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            var result = await new DataAnnotationsWorkflowValidator<T>().ValidateAsync(data);

            foundry.Properties[propertyKey] = result;
            foundry.Properties[$"{propertyKey}.IsValid"] = result.IsValid;

            if (!result.IsValid)
            {
                foundry.Properties[$"{propertyKey}.Errors"] = result.Errors;
            }

            return result;
        }

        /// <summary>
        /// Validates data using a custom workflow validator and stores the result in foundry properties.
        /// </summary>
        /// <typeparam name="T">The type of data to validate.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="validator">The workflow validator.</param>
        /// <param name="data">The data to validate.</param>
        /// <param name="propertyKey">The foundry property key to store the validation result.</param>
        /// <returns>A task representing the validation result.</returns>
        public static async System.Threading.Tasks.Task<ValidationResult> ValidateAsync<T>(
            this IWorkflowFoundry foundry,
            IWorkflowValidator<T> validator,
            T data,
            string propertyKey = "ValidationResult")
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (validator == null) throw new ArgumentNullException(nameof(validator));

            var result = await validator.ValidateAsync(data);

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