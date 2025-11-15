using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Persistence.Options
{
    /// <summary>
    /// Configuration options for Persistence middleware.
    /// Controls workflow state persistence behavior.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class PersistenceMiddlewareOptions
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Persistence";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// Can be customized via constructor for non-standard configuration layouts.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public PersistenceMiddlewareOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public PersistenceMiddlewareOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets whether persistence middleware is enabled.
        /// When true, workflow state is persisted to the configured provider.
        /// When false, middleware is not registered.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to persist workflow state after each operation completes.
        /// When true, creates checkpoints after each operation.
        /// When false, only persists on workflow completion or failure.
        /// Default is true.
        /// </summary>
        public bool PersistOnOperationComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to persist workflow state when workflow completes successfully.
        /// When true, final workflow state is persisted.
        /// When false, only intermediate state is persisted.
        /// Default is true.
        /// </summary>
        public bool PersistOnWorkflowComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to persist workflow state when workflow fails.
        /// When true, failure state is persisted for recovery.
        /// When false, failure state is not persisted.
        /// Default is true.
        /// </summary>
        public bool PersistOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to compress persisted data.
        /// When true, uses compression to reduce storage footprint.
        /// When false, persists raw data.
        /// Default is false.
        /// </summary>
        public bool CompressData { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of persisted versions to retain per workflow.
        /// 0 = unlimited, otherwise older versions are automatically purged.
        /// Default is 10.
        /// </summary>
        public int MaxVersions { get; set; } = 10;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();
            
            if (MaxVersions < 0)
            {
                errors.Add($"{SectionName}:MaxVersions must be >= 0 (current value: {MaxVersions})");
            }
            
            return errors;
        }
    }
}



