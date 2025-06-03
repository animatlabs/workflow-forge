using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Represents the result of a health check execution.
    /// </summary>
    public sealed class HealthCheckResult
    {
        /// <summary>
        /// Gets the status of the health check.
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the description of the health check result.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets additional data associated with the health check result.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Data { get; }

        /// <summary>
        /// Gets the exception that occurred during the health check, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the duration of the health check execution.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckResult class.
        /// </summary>
        /// <param name="status">The health status.</param>
        /// <param name="description">The optional description.</param>
        /// <param name="exception">The optional exception.</param>
        /// <param name="data">The optional additional data.</param>
        /// <param name="duration">The execution duration.</param>
        public HealthCheckResult(
            HealthStatus status,
            string? description = null,
            Exception? exception = null,
            IReadOnlyDictionary<string, object>? data = null,
            TimeSpan duration = default)
        {
            Status = status;
            Description = description;
            Exception = exception;
            Data = data;
            Duration = duration;
        }

        /// <summary>
        /// Creates a healthy result.
        /// </summary>
        /// <param name="description">The optional description.</param>
        /// <param name="data">The optional additional data.</param>
        /// <returns>A healthy health check result.</returns>
        public static HealthCheckResult Healthy(string? description = null, IReadOnlyDictionary<string, object>? data = null)
            => new(HealthStatus.Healthy, description, data: data);

        /// <summary>
        /// Creates a degraded result.
        /// </summary>
        /// <param name="description">The optional description.</param>
        /// <param name="exception">The optional exception.</param>
        /// <param name="data">The optional additional data.</param>
        /// <returns>A degraded health check result.</returns>
        public static HealthCheckResult Degraded(string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
            => new(HealthStatus.Degraded, description, exception, data);

        /// <summary>
        /// Creates an unhealthy result.
        /// </summary>
        /// <param name="description">The optional description.</param>
        /// <param name="exception">The optional exception.</param>
        /// <param name="data">The optional additional data.</param>
        /// <returns>An unhealthy health check result.</returns>
        public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
            => new(HealthStatus.Unhealthy, description, exception, data);
    }

    /// <summary>
    /// Represents the status of a health check.
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// The component is healthy.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// The component is degraded but still functional.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// The component is unhealthy.
        /// </summary>
        Unhealthy = 2
    }
} 
