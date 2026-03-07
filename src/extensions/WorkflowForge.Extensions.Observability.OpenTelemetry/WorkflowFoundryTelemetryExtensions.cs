using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry
{
    /// <summary>
    /// Configuration options for WorkflowForge OpenTelemetry integration.
    /// Supports both distributed tracing and metrics collection.
    /// </summary>
    public sealed class WorkflowForgeOpenTelemetryOptions
    {
        /// <summary>
        /// Gets or sets the service name for OpenTelemetry resource identification.
        /// </summary>
        public string ServiceName { get; set; } = "WorkflowForge.Service";

        /// <summary>
        /// Gets or sets the service version.
        /// </summary>
        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets whether distributed tracing is enabled.
        /// </summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets whether metrics collection is enabled.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether system metrics are collected automatically.
        /// </summary>
        public bool EnableSystemMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether operation metrics are recorded automatically.
        /// </summary>
        public bool EnableOperationMetrics { get; set; } = true;
    }

    /// <summary>
    /// Extension methods that add comprehensive OpenTelemetry observability capabilities to WorkflowFoundry.
    /// Provides both distributed tracing and metrics in a unified experience.
    /// </summary>
    public static class WorkflowFoundryOpenTelemetryExtensions
    {
        private const string OpenTelemetryServiceKey = "_opentelemetry_service";

        /// <summary>
        /// Gets the OpenTelemetry service from the foundry.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>The OpenTelemetry service or null if not available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static WorkflowForgeOpenTelemetryService? GetOpenTelemetryService(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            return foundry.Properties.TryGetValue(OpenTelemetryServiceKey, out var serviceObj) && serviceObj is WorkflowForgeOpenTelemetryService service ? service : null;
        }

        /// <summary>
        /// Enables comprehensive OpenTelemetry observability for the foundry.
        /// Includes both distributed tracing and metrics collection.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="options">Configuration options for OpenTelemetry.</param>
        /// <returns>True if OpenTelemetry was enabled successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static bool EnableOpenTelemetry(this IWorkflowFoundry foundry, WorkflowForgeOpenTelemetryOptions? options = null)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            try
            {
                options ??= new WorkflowForgeOpenTelemetryOptions();

                var service = new WorkflowForgeOpenTelemetryService(
                    options.ServiceName,
                    options.ServiceVersion,
                    foundry.Logger);

                foundry.Properties[OpenTelemetryServiceKey] = service;

                foundry.Logger.LogInformation(
                    "OpenTelemetry enabled for foundry with service name '{ServiceName}', tracing: {TracingEnabled}, metrics: {MetricsEnabled}",
                    options.ServiceName, options.EnableTracing, options.EnableMetrics);

                return true;
            }
            catch (Exception ex)
            {
                foundry.Logger.LogError(ex, "Failed to enable OpenTelemetry for foundry");
                return false;
            }
        }

        /// <summary>
        /// Disables OpenTelemetry observability for the foundry.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>True if OpenTelemetry was disabled successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static bool DisableOpenTelemetry(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            try
            {
                if (foundry.Properties.TryGetValue(OpenTelemetryServiceKey, out var serviceObj) && serviceObj is WorkflowForgeOpenTelemetryService service)
                {
                    service.Dispose();
                    // Remove from foundry properties
                    foundry.Properties[OpenTelemetryServiceKey] = null;
                    foundry.Logger.LogInformation("OpenTelemetry disabled for foundry");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                foundry.Logger.LogError(ex, "Failed to disable OpenTelemetry for foundry");
                return false;
            }
        }

        /// <summary>
        /// Starts a new distributed tracing activity (span) for an operation.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="kind">The activity kind.</param>
        /// <returns>The started activity or null if tracing is not available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static Activity? StartActivity(this IWorkflowFoundry foundry, string operationName, ActivityKind kind = ActivityKind.Internal)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            return service?.StartActivity(operationName, kind);
        }

        /// <summary>
        /// Records comprehensive metrics for an operation execution.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="duration">The operation duration.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="memoryAllocated">Memory allocated during the operation.</param>
        /// <param name="tags">Additional tags for the metrics.</param>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static void RecordOperationMetrics(
            this IWorkflowFoundry foundry,
            string operationName,
            TimeSpan duration,
            bool success,
            long memoryAllocated = 0,
            params (string Key, object? Value)[] tags)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            if (service == null)
                return;

            service.RecordOperation(operationName, duration, success, memoryAllocated, ToTagArray(tags));
        }

        /// <summary>
        /// Increments the active operations counter for tracking concurrent operations.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="tags">Additional tags for the metric.</param>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static void IncrementActiveOperations(
            this IWorkflowFoundry foundry,
            string operationName,
            params (string Key, object? Value)[] tags)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            if (service == null)
                return;

            service.IncrementActiveOperations(operationName, ToTagArray(tags));
        }

        /// <summary>
        /// Decrements the active operations counter when operations complete.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="tags">Additional tags for the metric.</param>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static void DecrementActiveOperations(
            this IWorkflowFoundry foundry,
            string operationName,
            params (string Key, object? Value)[] tags)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            if (service == null)
                return;

            service.DecrementActiveOperations(operationName, ToTagArray(tags));
        }

        private static KeyValuePair<string, object?>[]? ToTagArray((string Key, object? Value)[]? tags)
        {
            if (tags == null || tags.Length == 0)
                return null;

            var result = new KeyValuePair<string, object?>[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                result[i] = new KeyValuePair<string, object?>(tags[i].Key, tags[i].Value);
            }
            return result;
        }

        /// <summary>
        /// Creates a custom counter metric through the OpenTelemetry service.
        /// </summary>
        /// <typeparam name="T">The counter value type.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="name">The counter name.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The counter description.</param>
        /// <returns>The counter instrument or null if OpenTelemetry is not available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static Counter<T>? CreateCounter<T>(this IWorkflowFoundry foundry, string name, string? unit = null, string? description = null)
            where T : struct
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            return service?.CreateCounter<T>(name, unit, description);
        }

        /// <summary>
        /// Creates a custom histogram metric through the OpenTelemetry service.
        /// </summary>
        /// <typeparam name="T">The histogram value type.</typeparam>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="name">The histogram name.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The histogram description.</param>
        /// <returns>The histogram instrument or null if OpenTelemetry is not available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static Histogram<T>? CreateHistogram<T>(this IWorkflowFoundry foundry, string name, string? unit = null, string? description = null)
            where T : struct
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var service = foundry.GetOpenTelemetryService();
            return service?.CreateHistogram<T>(name, unit, description);
        }

        /// <summary>
        /// Checks if OpenTelemetry observability is currently enabled for the foundry.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>True if OpenTelemetry is enabled; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static bool IsOpenTelemetryEnabled(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            return foundry.GetOpenTelemetryService() != null;
        }
    }
}
