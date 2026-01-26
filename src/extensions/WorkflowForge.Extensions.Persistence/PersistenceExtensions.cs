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
        /// Adds persistence checkpoints after each operation using the provided persistence provider with options.
        /// Restores foundry properties and skips already-completed operations when resuming.
        /// </summary>
        /// <param name="foundry">The foundry to enable persistence on.</param>
        /// <param name="provider">The persistence provider that manages snapshots.</param>
        /// <param name="options">Configuration options for persistence behavior.</param>
        /// <returns>The same <see cref="IWorkflowFoundry"/> for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="foundry"/> or <paramref name="provider"/> is null.</exception>
        public static IWorkflowFoundry UsePersistence(
            this IWorkflowFoundry foundry,
            IWorkflowPersistenceProvider provider,
            PersistenceOptions? options = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            options ??= new PersistenceOptions();

            if (!options.Enabled)
            {
                foundry.Logger.LogInformation("Persistence middleware is disabled via configuration");
                return foundry;
            }

            foundry.AddMiddleware(new PersistenceMiddleware(provider, options));
            return foundry;
        }
    }
}