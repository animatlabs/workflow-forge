using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Persistence.Recovery.Options
{
    /// <summary>
    /// Configuration options for Recovery middleware.
    /// Controls workflow recovery and retry behavior.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class RecoveryMiddlewareOptions
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Recovery";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// Can be customized via constructor for non-standard configuration layouts.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public RecoveryMiddlewareOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public RecoveryMiddlewareOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets whether recovery middleware is enabled.
        /// When true, workflow recovery and retry logic is active.
        /// When false, middleware is not registered.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts before surfacing the last error.
        /// Must be between 1 and 100.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retry attempts.
        /// When exponential backoff is enabled, this is the initial delay.
        /// Must be greater than or equal to 0 and less than or equal to 10 minutes.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retry delays.
        /// When true, delay doubles with each attempt starting from BaseDelay.
        /// When false, uses fixed BaseDelay for all attempts.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to attempt workflow resumption from last checkpoint.
        /// When true, tries to resume from persisted state before fresh execution.
        /// When false, always starts fresh execution.
        /// Default is true.
        /// </summary>
        public bool AttemptResume { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log recovery attempts.
        /// When true, each retry attempt is logged for debugging.
        /// When false, only final success/failure is logged.
        /// Default is true.
        /// </summary>
        public bool LogRecoveryAttempts { get; set; } = true;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();
            
            if (MaxRetryAttempts < 1 || MaxRetryAttempts > 100)
            {
                errors.Add($"{SectionName}:MaxRetryAttempts must be between 1 and 100 (current value: {MaxRetryAttempts})");
            }
            
            if (BaseDelay < TimeSpan.Zero || BaseDelay > TimeSpan.FromMinutes(10))
            {
                errors.Add($"{SectionName}:BaseDelay must be between 0 and 10 minutes (current value: {BaseDelay.TotalSeconds}s)");
            }
            
            return errors;
        }
    }
}

