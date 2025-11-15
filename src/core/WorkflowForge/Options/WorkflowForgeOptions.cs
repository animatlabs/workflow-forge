using System;
using System.Collections.Generic;

namespace WorkflowForge.Options
{
    /// <summary>
    /// Configuration options for core WorkflowForge functionality.
    /// Zero-dependency POCO for configuration binding.
    /// Implements <see cref="ICloneable"/> for creating independent copies of configuration.
    /// </summary>
    public sealed class WorkflowForgeOptions : ICloneable
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// Can be customized via constructor for non-standard configuration layouts.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public WorkflowForgeOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public WorkflowForgeOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets the maximum number of workflows that can execute simultaneously.
        /// This throttles workflow-level parallelism when running multiple workflows with Task.WhenAll.
        /// 0 = unlimited (no throttling), otherwise must be between 1 and 10000.
        /// Default is 0 (unlimited).
        /// </summary>
        public int MaxConcurrentWorkflows { get; set; } = 0;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            // MaxConcurrentWorkflows: 0 = unlimited, 1-10000
            if (MaxConcurrentWorkflows < 0 || MaxConcurrentWorkflows > 10000)
            {
                errors.Add($"{SectionName}:MaxConcurrentWorkflows must be between 0 and 10000 (0 = unlimited, current value: {MaxConcurrentWorkflows})");
            }

            return errors;
        }

        /// <summary>
        /// Creates a shallow copy of the current options instance.
        /// Implements <see cref="ICloneable.Clone"/>.
        /// </summary>
        /// <returns>A new <see cref="WorkflowForgeOptions"/> instance with copied property values.</returns>
        public object Clone()
        {
            return new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = MaxConcurrentWorkflows
            };
        }

        /// <summary>
        /// Creates a strongly-typed copy of the current options.
        /// Convenience method that returns <see cref="WorkflowForgeOptions"/> instead of <see cref="object"/>.
        /// </summary>
        /// <returns>A new <see cref="WorkflowForgeOptions"/> instance with copied property values.</returns>
        public WorkflowForgeOptions CloneTyped()
        {
            return (WorkflowForgeOptions)Clone();
        }
    }
}