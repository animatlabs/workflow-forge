using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Extensions that provide recovery orchestration on top of persistence.
    /// Attempts to resume from a snapshot first, then runs a fresh execution with configurable retries.
    /// </summary>
    public static class RecoveryExtensions
    {
        /// <summary>
        /// Attempts to resume a workflow from the last checkpoint before starting a new execution.
        /// If no snapshot exists, proceeds to execute normally.
        /// </summary>
        public static async Task ForgeWithRecoveryAsync(
            this IWorkflowSmith smith,
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            IWorkflowPersistenceProvider provider,
            Guid foundryKey,
            Guid workflowKey,
            RecoveryPolicy? policy = null,
            CancellationToken cancellationToken = default)
        {
            if (smith == null) throw new ArgumentNullException(nameof(smith));
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            // Try recovery first
            var coordinator = new RecoveryCoordinator(provider, policy);
            // Phase 1: attempt resume (best-effort). If it throws, swallow here and move to fresh execution retries.
            try
            {
                await coordinator.ResumeAsync(
                    foundryFactory: () => foundry,
                    workflowFactory: () => workflow,
                    foundryKey: foundryKey,
                    workflowKey: workflowKey,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore and proceed to fresh execution attempts
            }

            // Ensure non-null policy instance
            policy ??= new RecoveryPolicy();

            // Phase 2: attempt a fresh execution with retry policy
            var attempts = 0;
            Exception? lastEx = null;
            while (attempts < policy.MaxAttempts)
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
                    if (attempts >= policy.MaxAttempts) break;

                    var delay = policy.BaseDelay;
                    if (policy.UseExponentialBackoff)
                    {
                        var factor = Math.Pow(2, attempts - 1);
                        delay = TimeSpan.FromMilliseconds(policy.BaseDelay.TotalMilliseconds * factor);
                    }
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastEx != null) throw lastEx;
        }
    }
}


