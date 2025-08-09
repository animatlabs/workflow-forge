using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Extension methods that add health check capabilities to WorkflowFoundry.
    /// These extensions provide simple, practical health monitoring functionality.
    /// </summary>
    public static class WorkflowFoundryHealthCheckExtensions
    {
        /// <summary>
        /// Creates a new health check service for monitoring workflow components.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="checkInterval">Optional interval for periodic health checks. If null, periodic checks are disabled.</param>
        /// <returns>A new health check service instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static HealthCheckService CreateHealthCheckService(this IWorkflowFoundry foundry, TimeSpan? checkInterval = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            // Use the foundry's logger, which is always available
            return new HealthCheckService(foundry.Logger, timeProvider: null, checkInterval);
        }

        /// <summary>
        /// Checks the health of the foundry and its components using a health check service.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="healthCheckService">The health check service to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The overall health status of the foundry.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry or healthCheckService is null.</exception>
        public static async Task<HealthStatus> CheckFoundryHealthAsync(this IWorkflowFoundry foundry, HealthCheckService healthCheckService, CancellationToken cancellationToken = default)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (healthCheckService == null) throw new ArgumentNullException(nameof(healthCheckService));

            await healthCheckService.CheckHealthAsync(cancellationToken);
            return healthCheckService.OverallStatus;
        }
    }
}