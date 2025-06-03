using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Configuration settings for WorkflowForge performance monitoring.
    /// Provides settings for controlling performance data collection and analysis.
    /// </summary>
    public sealed class PerformanceSettings : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the maximum degree of parallelism for parallel operations.
        /// </summary>
        [Range(1, 100, ErrorMessage = "MaxDegreeOfParallelism must be between 1 and 100")]
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets a value indicating whether to enable object pooling.
        /// </summary>
        public bool EnableObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of operations to queue.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "MaxQueuedOperations must be between 1 and 10,000")]
        public int MaxQueuedOperations { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the size of operation batches for batch processing.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "BatchSize must be between 1 and 1,000")]
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether to enable memory optimization features.
        /// </summary>
        public bool EnableMemoryOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets the garbage collection mode preference.
        /// </summary>
        public string GarbageCollectionMode { get; set; } = "Balanced";

        /// <summary>
        /// Validates the performance settings.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            var validGcModes = new[] { "Balanced", "LowLatency", "HighThroughput" };
            if (!Array.Exists(validGcModes, mode => mode.Equals(GarbageCollectionMode, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new ValidationResult(
                    $"GarbageCollectionMode must be one of: {string.Join(", ", validGcModes)}",
                    new[] { nameof(GarbageCollectionMode) }));
            }

            return results;
        }

        /// <summary>
        /// Creates a deep copy of the current performance settings.
        /// </summary>
        /// <returns>A new PerformanceSettings instance with copied values.</returns>
        public PerformanceSettings Clone()
        {
            return new PerformanceSettings
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                EnableObjectPooling = EnableObjectPooling,
                MaxQueuedOperations = MaxQueuedOperations,
                BatchSize = BatchSize,
                EnableMemoryOptimization = EnableMemoryOptimization,
                GarbageCollectionMode = GarbageCollectionMode
            };
        }
    }
} 
