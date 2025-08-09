namespace WorkflowForge.Operations
{
    /// <summary>
    /// Defines log levels for workflow operations.
    /// </summary>
    public enum WorkflowForgeLogLevel
    {
        /// <summary>
        /// Trace level logging - most detailed messages.
        /// </summary>
        Trace,

        /// <summary>
        /// Debug level logging - detailed information for debugging.
        /// </summary>
        Debug,

        /// <summary>
        /// Information level logging - general information about workflow progress.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level logging - potentially harmful situations.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level logging - error events that might still allow the workflow to continue.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level logging - very severe error events that might cause the workflow to abort.
        /// </summary>
        Critical
    }
}