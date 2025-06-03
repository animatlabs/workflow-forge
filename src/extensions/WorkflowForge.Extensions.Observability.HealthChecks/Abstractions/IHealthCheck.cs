using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Represents a health check that can be executed to determine the health status of a component.
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of what this health check validates.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the health check result.</returns>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
} 
