using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Extensions that provide recovery orchestration on top of persistence.
    /// Attempts to resume from a snapshot first, then runs a fresh execution with configurable retries.
    /// </summary>
    public static class RecoveryExtensions
    {
        /// <summary>
        /// Attempts to resume a workflow from the last checkpoint before starting a new execution with options.
        /// If no snapshot exists, proceeds to execute normally.
        /// </summary>
        public static async Task ForgeWithRecoveryAsync(
            this IWorkflowSmith smith,
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            IWorkflowPersistenceProvider provider,
            Guid foundryKey,
            Guid workflowKey,
            RecoveryMiddlewareOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (smith == null) throw new ArgumentNullException(nameof(smith));
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            options ??= new RecoveryMiddlewareOptions();

            if (!options.Enabled)
            {
                foundry.Logger.LogInformation("Recovery is disabled via configuration, executing without recovery");
                await smith.ForgeAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Try recovery first
            var coordinator = new RecoveryCoordinator(provider, options);
            // Phase 1: attempt resume (best-effort). If it throws, swallow here and move to fresh execution retries.
            try
            {
                await coordinator.ResumeAsync(
                    foundryFactory: () => foundry,
                    workflowFactory: () => workflow,
                    foundryKey: foundryKey,
                    workflowKey: workflowKey,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return; // Success
            }
            catch (Exception ex)
            {
                foundry.Logger.LogWarning(ex, "Recovery resume failed, proceeding to fresh execution attempts");
            }

            // Phase 2: attempt a fresh execution with retry options
            var attempts = 0;
            Exception? lastEx = null;
            while (attempts < options.MaxRetryAttempts)
            {
                try
                {
                    await smith.ForgeAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    attempts++;
                    if (attempts >= options.MaxRetryAttempts) break;

                    var delay = options.BaseDelay;
                    if (options.UseExponentialBackoff)
                    {
                        var factor = Math.Pow(2, attempts - 1);
                        delay = TimeSpan.FromMilliseconds(options.BaseDelay.TotalMilliseconds * factor);
                    }
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastEx != null) throw lastEx;
        }
    }
}