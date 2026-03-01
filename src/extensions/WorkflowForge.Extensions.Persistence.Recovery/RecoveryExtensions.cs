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
            ValidateForgeWithRecoveryArgs(smith, workflow, foundry, provider);
            options ??= new RecoveryMiddlewareOptions();

            if (!options.Enabled)
            {
                foundry.Logger.LogInformation("Recovery is disabled via configuration, executing without recovery");
                await smith.ForgeAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (await TryResumeFromSnapshotAsync(coordinator: new RecoveryCoordinator(provider, options), foundry, workflow, foundryKey, workflowKey, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await ExecuteWithRetriesAsync(smith, workflow, foundry, options, cancellationToken).ConfigureAwait(false);
        }

        private static void ValidateForgeWithRecoveryArgs(IWorkflowSmith? smith, IWorkflow? workflow, IWorkflowFoundry? foundry, IWorkflowPersistenceProvider? provider)
        {
            if (smith == null)
                throw new ArgumentNullException(nameof(smith));
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
        }

        private static async Task<bool> TryResumeFromSnapshotAsync(
            RecoveryCoordinator coordinator,
            IWorkflowFoundry foundry,
            IWorkflow workflow,
            Guid foundryKey,
            Guid workflowKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await coordinator.ResumeAsync(
                    foundryFactory: () => foundry,
                    workflowFactory: () => workflow,
                    foundryKey: foundryKey,
                    workflowKey: workflowKey,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                foundry.Logger.LogWarning(ex, "Recovery resume failed, proceeding to fresh execution attempts");
                return false;
            }
        }

        private static async Task ExecuteWithRetriesAsync(
            IWorkflowSmith smith,
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            RecoveryMiddlewareOptions options,
            CancellationToken cancellationToken)
        {
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
                    if (attempts >= options.MaxRetryAttempts)
                        break;

                    var delay = GetRetryDelay(options, attempts);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastEx != null)
                throw lastEx;
        }

        private static TimeSpan GetRetryDelay(RecoveryMiddlewareOptions options, int attempts)
        {
            if (!options.UseExponentialBackoff)
            {
                return options.BaseDelay;
            }

            var factor = Math.Pow(2, attempts - 1);
            return TimeSpan.FromMilliseconds(options.BaseDelay.TotalMilliseconds * factor);
        }
    }
}
