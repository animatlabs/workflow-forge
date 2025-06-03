using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Service that manages and executes health checks for the WorkflowForge system.
    /// </summary>
    public sealed class HealthCheckService : IDisposable
    {
        private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
        private readonly IWorkflowForgeLogger _logger;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly Timer? _periodicCheckTimer;
        private readonly TimeSpan _checkInterval;
        private volatile bool _disposed;

        /// <summary>
        /// Gets the results of the last health check execution.
        /// </summary>
        public IReadOnlyDictionary<string, HealthCheckResult> LastResults { get; private set; } = 
            new Dictionary<string, HealthCheckResult>();

        /// <summary>
        /// Gets the overall health status based on the last check results.
        /// </summary>
        public HealthStatus OverallStatus
        {
            get
            {
                if (!LastResults.Any())
                    return HealthStatus.Healthy;

                if (LastResults.Values.Any(r => r.Status == HealthStatus.Unhealthy))
                    return HealthStatus.Unhealthy;

                if (LastResults.Values.Any(r => r.Status == HealthStatus.Degraded))
                    return HealthStatus.Degraded;

                return HealthStatus.Healthy;
            }
        }

        /// <summary>
        /// Initializes a new instance of the HealthCheckService class.
        /// </summary>
        /// <param name="logger">The logger to use for health check operations.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        /// <param name="checkInterval">The interval for periodic health checks. Set to null to disable periodic checks.</param>
        /// <param name="registerBuiltInHealthChecks">Whether to automatically register built-in health checks.</param>
        public HealthCheckService(IWorkflowForgeLogger logger, ISystemTimeProvider? timeProvider = null, TimeSpan? checkInterval = null, bool registerBuiltInHealthChecks = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);

            if (registerBuiltInHealthChecks)
            {
                RegisterBuiltInHealthChecks();
            }

            if (checkInterval.HasValue)
            {
                _periodicCheckTimer = new Timer(PeriodicHealthCheck, null, _checkInterval, _checkInterval);
                
                var startupProperties = new Dictionary<string, string>
                {
                    [PropertyNames.ExecutionType] = "HealthCheckService",
                    [HealthCheckPropertyNames.MonitoringIntervalMs] = _checkInterval.TotalMilliseconds.ToString("F0")
                };
                
                _logger.LogInformation(startupProperties, HealthCheckLogMessages.HealthCheckServiceStarted);
            }
        }

        /// <summary>
        /// Registers a health check with the service.
        /// </summary>
        /// <param name="healthCheck">The health check to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when healthCheck is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public void RegisterHealthCheck(IHealthCheck healthCheck)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckService));
            if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));

            _healthChecks.AddOrUpdate(healthCheck.Name, healthCheck, (key, existing) => healthCheck);
            
            var registrationProperties = new Dictionary<string, string>
            {
                [HealthCheckPropertyNames.HealthCheckName] = healthCheck.Name,
                [HealthCheckPropertyNames.TotalHealthChecks] = _healthChecks.Count.ToString()
            };
            
            _logger.LogInformation(registrationProperties, HealthCheckLogMessages.HealthCheckRegistered);
        }

        /// <summary>
        /// Unregisters a health check from the service.
        /// </summary>
        /// <param name="name">The name of the health check to remove.</param>
        /// <returns>True if the health check was removed; otherwise, false.</returns>
        public bool UnregisterHealthCheck(string name)
        {
            if (_disposed || string.IsNullOrWhiteSpace(name)) return false;

            var removed = _healthChecks.TryRemove(name, out _);
            if (removed)
            {
                var unregistrationProperties = new Dictionary<string, string>
                {
                    [HealthCheckPropertyNames.HealthCheckName] = name,
                    [HealthCheckPropertyNames.TotalHealthChecks] = _healthChecks.Count.ToString()
                };
                
                _logger.LogInformation(unregistrationProperties, HealthCheckLogMessages.HealthCheckUnregistered);
            }
            return removed;
        }

        /// <summary>
        /// Executes all registered health checks asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary containing the results of all health checks.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public async Task<IReadOnlyDictionary<string, HealthCheckResult>> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckService));
            cancellationToken.ThrowIfCancellationRequested();

            var results = new Dictionary<string, HealthCheckResult>();
            var tasks = new List<Task<(string Name, HealthCheckResult Result)>>();

            foreach (var healthCheck in _healthChecks.Values)
            {
                tasks.Add(ExecuteHealthCheckAsync(healthCheck, cancellationToken));
            }

            if (tasks.Count == 0)
            {
                var noChecksProperties = new Dictionary<string, string>
                {
                    [HealthCheckPropertyNames.TotalHealthChecks] = "0"
                };
                
                _logger.LogWarning(noChecksProperties, HealthCheckLogMessages.NoHealthChecksRegistered);
                return results;
            }

            try
            {
                var completedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
                
                foreach (var (name, result) in completedTasks)
                {
                    results[name] = result;
                }

                LastResults = results;
                
                var overallStatus = OverallStatus;
                var healthyCounts = results.Values.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
                
                var completionProperties = new Dictionary<string, string>
                {
                    [HealthCheckPropertyNames.OverallHealthStatus] = overallStatus.ToString(),
                    [HealthCheckPropertyNames.TotalHealthChecks] = results.Count.ToString(),
                    [HealthCheckPropertyNames.HealthyCount] = (healthyCounts.TryGetValue(HealthStatus.Healthy, out var healthy) ? healthy : 0).ToString(),
                    [HealthCheckPropertyNames.DegradedCount] = (healthyCounts.TryGetValue(HealthStatus.Degraded, out var degraded) ? degraded : 0).ToString(),
                    [HealthCheckPropertyNames.UnhealthyCount] = (healthyCounts.TryGetValue(HealthStatus.Unhealthy, out var unhealthy) ? unhealthy : 0).ToString()
                };

                _logger.LogInformation(completionProperties, HealthCheckLogMessages.AllHealthChecksCompleted);

                return results;
            }
            catch (Exception ex)
            {
                var errorProperties = LoggingContextHelper.CreateErrorProperties(ex, "HealthCheckExecution");
                _logger.LogError(errorProperties, ex, HealthCheckLogMessages.HealthChecksCompletionFailed);
                throw;
            }
        }

        /// <summary>
        /// Executes a specific health check by name.
        /// </summary>
        /// <param name="name">The name of the health check to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result, or null if the health check is not found.</returns>
        public async Task<HealthCheckResult?> CheckHealthAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckService));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));

            if (!_healthChecks.TryGetValue(name, out var healthCheck))
            {
                var notFoundProperties = new Dictionary<string, string>
                {
                    [HealthCheckPropertyNames.HealthCheckName] = name
                };
                
                _logger.LogWarning(notFoundProperties, HealthCheckLogMessages.HealthCheckNotFound);
                return null;
            }

            var (_, result) = await ExecuteHealthCheckAsync(healthCheck, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private async Task<(string Name, HealthCheckResult Result)> ExecuteHealthCheckAsync(IHealthCheck healthCheck, CancellationToken cancellationToken)
        {
            var startTime = _timeProvider.UtcNow;
            
            var executionProperties = new Dictionary<string, string>
            {
                [HealthCheckPropertyNames.HealthCheckName] = healthCheck.Name,
                [PropertyNames.ExecutionType] = "HealthCheck"
            };

            using var healthCheckScope = _logger.BeginScope("HealthCheckExecution", executionProperties);
            
            try
            {
                _logger.LogDebug(HealthCheckLogMessages.HealthCheckExecutionStarted);
                
                var result = await healthCheck.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                var duration = _timeProvider.UtcNow - startTime;
                
                // Create a new result with the measured duration
                var resultWithDuration = new HealthCheckResult(
                    result.Status,
                    result.Description,
                    result.Exception,
                    result.Data,
                    duration);

                var completionProperties = new Dictionary<string, string>
                {
                    [HealthCheckPropertyNames.HealthStatus] = result.Status.ToString(),
                    [HealthCheckPropertyNames.HealthCheckDurationMs] = duration.TotalMilliseconds.ToString("F0")
                };

                _logger.LogDebug(completionProperties, HealthCheckLogMessages.HealthCheckExecutionCompleted);
                
                return (healthCheck.Name, resultWithDuration);
            }
            catch (Exception ex)
            {
                var duration = _timeProvider.UtcNow - startTime;
                var errorProperties = LoggingContextHelper.CreateErrorProperties(ex, "HealthCheck");
                errorProperties[HealthCheckPropertyNames.HealthCheckDurationMs] = duration.TotalMilliseconds.ToString("F0");
                
                _logger.LogError(errorProperties, ex, HealthCheckLogMessages.HealthCheckExecutionFailed);
                
                var errorResult = new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    $"Health check execution failed: {ex.Message}",
                    ex,
                    duration: duration);
                
                return (healthCheck.Name, errorResult);
            }
        }

        private void RegisterBuiltInHealthChecks()
        {
            RegisterHealthCheck(new MemoryHealthCheck());
            RegisterHealthCheck(new GarbageCollectorHealthCheck());
            RegisterHealthCheck(new ThreadPoolHealthCheck());
        }

        private void PeriodicHealthCheck(object? state)
        {
            if (_disposed) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckHealthAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var errorProperties = LoggingContextHelper.CreateErrorProperties(ex, "PeriodicHealthCheck");
                    _logger.LogError(errorProperties, ex, HealthCheckLogMessages.PeriodicHealthCheckFailed);
                }
            });
        }

        /// <summary>
        /// Disposes the health check service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _periodicCheckTimer?.Dispose();
            
            // Dispose health checks that implement IDisposable
            foreach (var healthCheck in _healthChecks.Values.OfType<IDisposable>())
            {
                try
                {
                    healthCheck.Dispose();
                }
                catch (Exception ex)
                {
                    var disposalProperties = new Dictionary<string, string>
                    {
                        [HealthCheckPropertyNames.HealthCheckName] = healthCheck.GetType().Name
                    };
                    var errorProperties = LoggingContextHelper.CreateErrorProperties(ex, "HealthCheckDisposal");
                    foreach (var kvp in disposalProperties)
                        errorProperties[kvp.Key] = kvp.Value;
                    
                    _logger.LogError(errorProperties, ex, HealthCheckLogMessages.HealthCheckDisposalError);
                }
            }
            
            _healthChecks.Clear();
            
            var serviceDisposalProperties = new Dictionary<string, string>
            {
                [PropertyNames.ExecutionType] = "HealthCheckService"
            };
            
            _logger.LogInformation(serviceDisposalProperties, HealthCheckLogMessages.HealthCheckServiceDisposed);
        }
    }
} 
