using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Middleware;

namespace WorkflowForge.Extensions
{
    /// <summary>
    /// Extensions to enable core logging via middleware.
    /// </summary>
    public static class WorkflowFoundryExtensions
    {
        /// <summary>
        /// Adds core logging using the foundry's current logger.
        /// </summary>
        public static IWorkflowFoundry UseLogging(this IWorkflowFoundry foundry)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.AddMiddleware(new LoggingMiddleware(foundry.Logger));
            return foundry;
        }

        /// <summary>
        /// Adds core logging using the provided logger.
        /// </summary>
        public static IWorkflowFoundry UseLogging(this IWorkflowFoundry foundry, IWorkflowForgeLogger logger)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            foundry.AddMiddleware(new LoggingMiddleware(logger));
            return foundry;
        }
    }
}