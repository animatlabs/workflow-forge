using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Extension methods to enable persistence using a user-provided provider.
    /// </summary>
    public static class PersistenceExtensions
    {
        /// <summary>
        /// Adds persistence checkpoints after each operation using the provided persistence provider.
        /// Restores foundry properties and skips already-completed operations when resuming.
        /// </summary>
        /// <param name="foundry">The foundry to enable persistence on.</param>
        /// <param name="provider">The persistence provider that manages snapshots.</param>
        /// <returns>The same <see cref="IWorkflowFoundry"/> for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="foundry"/> or <paramref name="provider"/> is null.</exception>
        public static IWorkflowFoundry UsePersistence(this IWorkflowFoundry foundry, IWorkflowPersistenceProvider provider)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            foundry.AddMiddleware(new PersistenceMiddleware(provider));
            return foundry;
        }

        /// <summary>
        /// Adds persistence with options to control stable keys for cross-process resume.
        /// </summary>
        public static IWorkflowFoundry UsePersistence(this IWorkflowFoundry foundry, IWorkflowPersistenceProvider provider, PersistenceOptions options)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (options == null) throw new ArgumentNullException(nameof(options));

            foundry.AddMiddleware(new PersistenceMiddleware(provider, options));
            return foundry;
        }
    }
}