using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Built-in health check for monitoring memory usage.
    /// </summary>
    public sealed class MemoryHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public string Name => "Memory";

        /// <summary>
        /// Gets the description of what this health check validates.
        /// </summary>
        public string Description => "Monitors process memory usage and availability";

        /// <summary>
        /// Executes the health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var managedMemory = GC.GetTotalMemory(forceFullCollection: false);

                var data = new Dictionary<string, object>
                {
                    ["WorkingSetMB"] = workingSet / (1024.0 * 1024.0),
                    ["GCTotalMemoryMB"] = managedMemory / (1024.0 * 1024.0)
                };

                // Basic thresholds (can be made configurable)
                var workingSetMB = workingSet / (1024.0 * 1024.0);

                HealthStatus status;
                string description;

                if (workingSetMB > 1000) // > 1GB
                {
                    status = HealthStatus.Unhealthy;
                    description = $"High memory usage: {workingSetMB:F1}MB working set";
                }
                else if (workingSetMB > 500) // > 500MB
                {
                    status = HealthStatus.Degraded;
                    description = $"Elevated memory usage: {workingSetMB:F1}MB working set";
                }
                else
                {
                    status = HealthStatus.Healthy;
                    description = $"Memory usage normal: {workingSetMB:F1}MB working set";
                }

                return Task.FromResult(new HealthCheckResult(status, description, data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check memory status", ex));
            }
        }
    }
}