using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Built-in health check for monitoring thread pool health.
    /// </summary>
    public sealed class ThreadPoolHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public string Name => "ThreadPool";
        
        /// <summary>
        /// Gets the description of what this health check validates.
        /// </summary>
        public string Description => "Monitors thread pool availability and usage";

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
                ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                
                var busyWorkerThreads = maxWorkerThreads - availableWorkerThreads;
                var busyCompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads;
                
                var data = new Dictionary<string, object>
                {
                    ["WorkerThreads"] = availableWorkerThreads,
                    ["CompletionPortThreads"] = availableCompletionPortThreads,
                    ["MaxWorkerThreads"] = maxWorkerThreads,
                    ["MaxCompletionPortThreads"] = maxCompletionPortThreads,
                    ["BusyWorkerThreads"] = busyWorkerThreads,
                    ["BusyCompletionPortThreads"] = busyCompletionPortThreads,
                    ["WorkerThreadUsagePercent"] = (double)busyWorkerThreads / maxWorkerThreads * 100
                };

                var workerUsagePercent = (double)busyWorkerThreads / maxWorkerThreads * 100;
                
                HealthStatus status;
                string description;
                
                if (workerUsagePercent > 90)
                {
                    status = HealthStatus.Unhealthy;
                    description = $"Thread pool exhausted: {workerUsagePercent:F1}% worker threads busy";
                }
                else if (workerUsagePercent > 75)
                {
                    status = HealthStatus.Degraded;
                    description = $"High thread pool usage: {workerUsagePercent:F1}% worker threads busy";
                }
                else
                {
                    status = HealthStatus.Healthy;
                    description = $"Thread pool healthy: {workerUsagePercent:F1}% worker threads busy";
                }

                return Task.FromResult(new HealthCheckResult(status, description, data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check thread pool status", ex));
            }
        }
    }
} 
