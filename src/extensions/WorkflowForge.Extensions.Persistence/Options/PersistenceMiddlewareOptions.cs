using System;
using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Persistence.Options
{
    /// <summary>
    /// Configuration options for Persistence middleware.
    /// Controls workflow state persistence behavior.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class PersistenceMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Persistence";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public PersistenceMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public PersistenceMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

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
        public override IList<string> Validate()
        {
            var errors = new List<string>();
            
            if (MaxVersions < 0)
            {
                errors.Add($"{SectionName}:MaxVersions must be >= 0 (current value: {MaxVersions})");
            }
            
            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public override object Clone()
        {
            return new PersistenceMiddlewareOptions(SectionName)
            {
                Enabled = Enabled,
                PersistOnOperationComplete = PersistOnOperationComplete,
                PersistOnWorkflowComplete = PersistOnWorkflowComplete,
                PersistOnFailure = PersistOnFailure,
                CompressData = CompressData,
                MaxVersions = MaxVersions
            };
        }
    }
}



