using System;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Container for foreach operation results.
    /// Holds the results from each operation executed in a foreach workflow operation.
    /// </summary>
    public sealed class ForEachResults
    {
        /// <summary>
        /// Gets or sets the results from each operation in the foreach.
        /// The array index corresponds to the operation index in the foreach operation.
        /// </summary>
        public object?[] Results { get; set; } = Array.Empty<object?>();

        /// <summary>
        /// Gets or sets the total number of results.
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the results were created.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the number of results.
        /// </summary>
        public int Count => Results.Length;

        /// <summary>
        /// Gets a result by index.
        /// </summary>
        /// <param name="index">The index of the result.</param>
        /// <returns>The result at the specified index, or null if index is out of range.</returns>
        public object? GetResult(int index)
        {
            return index >= 0 && index < Results.Length ? Results[index] : null;
        }

        /// <summary>
        /// Gets a strongly-typed result by index.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="index">The index of the result.</param>
        /// <returns>The result cast to T, or default(T) if not found or cannot be cast.</returns>
        public T? GetResult<T>(int index)
        {
            var result = GetResult(index);
            return result is T typedResult ? typedResult : default(T);
        }
    }
}