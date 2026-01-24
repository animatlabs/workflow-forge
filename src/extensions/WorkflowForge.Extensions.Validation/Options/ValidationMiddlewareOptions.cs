using System;
using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Validation.Options
{
    /// <summary>
    /// Configuration options for Validation middleware.
    /// Controls validation behavior and error handling.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class ValidationMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Validation";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public ValidationMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public ValidationMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

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
        public override IList<string> Validate()
        {
            var errors = new List<string>();
            
            // Logical validation: if ignoring failures but also throwing, that's a conflict
            if (IgnoreValidationFailures && ThrowOnValidationError)
            {
                errors.Add($"{SectionName}: Cannot have both IgnoreValidationFailures=true and ThrowOnValidationError=true");
            }
            
            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public override object Clone()
        {
            return new ValidationMiddlewareOptions(SectionName)
            {
                Enabled = Enabled,
                IgnoreValidationFailures = IgnoreValidationFailures,
                ThrowOnValidationError = ThrowOnValidationError,
                LogValidationErrors = LogValidationErrors,
                StoreValidationResults = StoreValidationResults
            };
        }
    }
}



