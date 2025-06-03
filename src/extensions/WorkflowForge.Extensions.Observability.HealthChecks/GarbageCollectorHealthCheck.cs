using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Built-in health check for monitoring garbage collection performance.
    /// </summary>
    public sealed class GarbageCollectorHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public string Name => "GC";
        
        /// <summary>
        /// Gets the description of what this health check validates.
        /// </summary>
        public string Description => "Monitors garbage collection performance and pressure";

        /// <summary>
        /// Executes the health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                var totalMemory = GC.GetTotalMemory(forceFullCollection: false);
                
                var data = new Dictionary<string, object>
                {
                    ["gen0_collections"] = gen0Collections,
                    ["gen1_collections"] = gen1Collections,
                    ["gen2_collections"] = gen2Collections,
                    ["total_memory_bytes"] = totalMemory,
                    ["total_memory_mb"] = totalMemory / (1024.0 * 1024.0)
                };

                // Assess GC pressure
                HealthStatus status;
                string description;
                
                // Simple heuristic: if Gen2 collections are frequent relative to Gen0
                var gen2Ratio = gen0Collections > 0 ? (double)gen2Collections / gen0Collections : 0;
                
                if (gen2Ratio > 0.1) // More than 10% Gen2 collections
                {
                    status = HealthStatus.Degraded;
                    description = $"High GC pressure: {gen2Collections} Gen2 collections ({gen2Ratio:P1} ratio)";
                }
                else if (gen2Ratio > 0.05) // More than 5% Gen2 collections
                {
                    status = HealthStatus.Degraded;
                    description = $"Moderate GC pressure: {gen2Collections} Gen2 collections ({gen2Ratio:P1} ratio)";
                }
                else
                {
                    status = HealthStatus.Healthy;
                    description = $"GC performance normal: {gen2Collections} Gen2 collections";
                }

                return Task.FromResult(new HealthCheckResult(status, description, data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check GC status", ex));
            }
        }
    }
} 
