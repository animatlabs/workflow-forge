namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Options to control persistence keys for resumable workflows across processes.
    /// </summary>
    public sealed class PersistenceOptions
    {
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
    }
}