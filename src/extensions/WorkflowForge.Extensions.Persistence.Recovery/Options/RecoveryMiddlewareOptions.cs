using System;
using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Persistence.Recovery.Options
{
    /// <summary>
    /// Configuration options for Recovery middleware.
    /// Controls workflow recovery and retry behavior.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class RecoveryMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Recovery";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public RecoveryMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public RecoveryMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

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
        public override IList<string> Validate()
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

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public override object Clone()
        {
            return new RecoveryMiddlewareOptions(SectionName)
            {
                Enabled = Enabled,
                MaxRetryAttempts = MaxRetryAttempts,
                BaseDelay = BaseDelay,
                UseExponentialBackoff = UseExponentialBackoff,
                AttemptResume = AttemptResume,
                LogRecoveryAttempts = LogRecoveryAttempts
            };
        }
    }
}