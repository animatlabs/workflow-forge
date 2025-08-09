namespace WorkflowForge.Operations
{
    /// <summary>
    /// Strategies for handling data distribution in foreach operations.
    /// Defines how input data is distributed among parallel operations.
    /// </summary>
    public enum ForEachDataStrategy
    {
        /// <summary>
        /// All operations receive the same input data.
        /// Use this when all operations need access to the complete input.
        /// </summary>
        SharedInput,

        /// <summary>
        /// Input data is split among operations (expects array/list input).
        /// Use this when you want to distribute different parts of the input to different operations.
        /// </summary>
        SplitInput,

        /// <summary>
        /// Operations receive no input data (null).
        /// Use this when operations are self-contained and don't need input data.
        /// </summary>
        NoInput
    }
}