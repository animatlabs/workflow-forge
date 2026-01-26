using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Middleware;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Extensions
{
    /// <summary>
    /// Extension methods for IWorkflowFoundry providing middleware configuration and setup.
    /// This file focuses on middleware pipeline configuration (UseLogging, UseTiming, UseErrorHandling).
    /// For property management and operation chaining, see <see cref="FoundryPropertyExtensions"/>.
    /// </summary>
    public static class FoundryMiddlewareExtensions
    {
        /// <summary>
        /// Adds core logging middleware using the foundry's current logger.
        /// </summary>
        public static IWorkflowFoundry UseLogging(this IWorkflowFoundry foundry)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.AddMiddleware(new LoggingMiddleware(foundry.Logger));
            return foundry;
        }

        /// <summary>
        /// Adds core logging middleware using the provided logger.
        /// </summary>
        public static IWorkflowFoundry UseLogging(this IWorkflowFoundry foundry, IWorkflowForgeLogger logger)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            foundry.AddMiddleware(new LoggingMiddleware(logger));
            return foundry;
        }

        /// <summary>
        /// Adds timing middleware for operation performance tracking.
        /// </summary>
        public static IWorkflowFoundry UseTiming(this IWorkflowFoundry foundry, ISystemTimeProvider? timeProvider = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.AddMiddleware(new TimingMiddleware(timeProvider));
            return foundry;
        }

        /// <summary>
        /// Adds error handling middleware with optional exception swallowing.
        /// </summary>
        public static IWorkflowFoundry UseErrorHandling(
            this IWorkflowFoundry foundry,
            bool rethrowExceptions = true,
            object? defaultReturnValue = null,
            ISystemTimeProvider? timeProvider = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.AddMiddleware(new ErrorHandlingMiddleware(
                foundry.Logger,
                rethrowExceptions,
                defaultReturnValue,
                timeProvider));
            return foundry;
        }

        /// <summary>
        /// Configures the foundry with default middleware based on individual middleware options.
        /// This is the recommended way to configure middleware for production use.
        /// Middleware will be added in the correct order: ErrorHandling → Timing → Logging.
        /// Only enabled middleware will be registered (checked at startup, not at runtime).
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="errorHandlingOptions">Error handling middleware options (if null, uses defaults).</param>
        /// <param name="timingOptions">Timing middleware options (if null, uses defaults).</param>
        /// <param name="loggingOptions">Logging middleware options (if null, uses defaults).</param>
        /// <param name="timeProvider">Optional time provider for timing/error handling middleware.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when options validation fails.</exception>
        public static IWorkflowFoundry UseDefaultMiddleware(
            this IWorkflowFoundry foundry,
            ErrorHandlingMiddlewareOptions? errorHandlingOptions = null,
            TimingMiddlewareOptions? timingOptions = null,
            LoggingMiddlewareOptions? loggingOptions = null,
            ISystemTimeProvider? timeProvider = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            // Use default options if none provided
            errorHandlingOptions ??= new ErrorHandlingMiddlewareOptions();
            timingOptions ??= new TimingMiddlewareOptions();
            loggingOptions ??= new LoggingMiddlewareOptions();

            // Validate logging options (only one with validation logic)
            var loggingErrors = loggingOptions.Validate();
            if (loggingErrors.Count > 0)
            {
                throw new ArgumentException(
                    $"Invalid logging middleware options: {string.Join("; ", loggingErrors)}",
                    nameof(loggingOptions));
            }

            // Add middleware in the correct order (outer to inner execution)
            // Only register middleware that is enabled (checked once at startup, not at runtime)

            // Error handling should be outermost to catch all exceptions
            if (errorHandlingOptions.Enabled)
            {
                foundry.AddMiddleware(new ErrorHandlingMiddleware(
                    foundry.Logger,
                    errorHandlingOptions,
                    defaultReturnValue: null,
                    timeProvider: timeProvider));
            }

            // Timing middleware should track total time including logging
            if (timingOptions.Enabled)
            {
                foundry.AddMiddleware(new TimingMiddleware(timingOptions, timeProvider));
            }

            // Logging middleware is innermost (logs actual operation execution)
            if (loggingOptions.Enabled)
            {
                foundry.AddMiddleware(new LoggingMiddleware(foundry.Logger, loggingOptions));
            }

            return foundry;
        }
    }
}
