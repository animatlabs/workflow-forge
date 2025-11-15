using System.Collections.Generic;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Configuration options for Persistence middleware.
    /// Controls workflow state persistence behavior and resumable workflows across processes.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class PersistenceOptions
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
        public PersistenceOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public PersistenceOptions(string sectionName)
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
        /// Gets or sets the maximum number of persisted versions to retain per workflow.
        /// 0 = unlimited, otherwise older versions are automatically purged.
        /// Default is 0 (unlimited).
        /// </summary>
        public int MaxVersions { get; set; } = 0;

        /// <summary>
        /// Optional stable instance identifier to correlate a foundry across process restarts.
        /// When set, a deterministic key will be used instead of the transient ExecutionId.
        /// </summary>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Optional stable workflow key to correlate a workflow definition across runs.
        /// When set, a deterministic key will be used instead of the auto-generated Workflow.Id.
        /// </summary>
        public string? WorkflowKey { get; set; }

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
