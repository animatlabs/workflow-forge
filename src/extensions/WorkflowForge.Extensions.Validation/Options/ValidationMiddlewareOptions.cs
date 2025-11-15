using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Validation.Options
{
    /// <summary>
    /// Configuration options for Validation middleware.
    /// Controls validation behavior and error handling.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class ValidationMiddlewareOptions
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Validation";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// Can be customized via constructor for non-standard configuration layouts.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public ValidationMiddlewareOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public ValidationMiddlewareOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets whether validation middleware is enabled.
        /// When true, FluentValidation validators are executed on workflow data.
        /// When false, middleware is not registered.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to ignore validation failures.
        /// When true, validation errors are logged but don't stop workflow execution.
        /// When false, validation failures throw WorkflowValidationException.
        /// Default is false (strict validation).
        /// </summary>
        public bool IgnoreValidationFailures { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to throw exceptions on validation errors.
        /// When true, throws WorkflowValidationException on validation failure.
        /// When false, logs error and continues (requires IgnoreValidationFailures = true).
        /// Default is true.
        /// </summary>
        public bool ThrowOnValidationError { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log validation errors.
        /// When true, validation errors are logged even if ignored.
        /// When false, validation errors are not logged (not recommended).
        /// Default is true.
        /// </summary>
        public bool LogValidationErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include validation details in foundry properties.
        /// When true, stores validation results in foundry.Properties["Validation.Status"] and ["Validation.Errors"].
        /// When false, validation results are only logged or thrown.
        /// Default is true.
        /// </summary>
        public bool StoreValidationResults { get; set; } = true;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();
            
            // Logical validation: if ignoring failures but also throwing, that's a conflict
            if (IgnoreValidationFailures && ThrowOnValidationError)
            {
                errors.Add($"{SectionName}: Cannot have both IgnoreValidationFailures=true and ThrowOnValidationError=true");
            }
            
            return errors;
        }
    }
}



