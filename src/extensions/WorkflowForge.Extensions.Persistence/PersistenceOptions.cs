using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Configuration options for Persistence middleware.
    /// Controls workflow state persistence behavior and resumable workflows across processes.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class PersistenceOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Persistence";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public PersistenceOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public PersistenceOptions(string sectionName) : base(sectionName, DefaultSectionName)
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
            return new PersistenceOptions(SectionName)
            {
                Enabled = Enabled,
                PersistOnOperationComplete = PersistOnOperationComplete,
                PersistOnWorkflowComplete = PersistOnWorkflowComplete,
                PersistOnFailure = PersistOnFailure,
                MaxVersions = MaxVersions,
                InstanceId = InstanceId,
                WorkflowKey = WorkflowKey
            };
        }
    }
}