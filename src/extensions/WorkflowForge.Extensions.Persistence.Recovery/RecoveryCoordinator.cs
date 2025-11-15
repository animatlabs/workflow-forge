using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Default implementation that uses IWorkflowPersistenceProvider to load snapshots and resume via smith.
    /// Foundry/workflow factories must reproduce the same operation order used to create the snapshot.
    /// </summary>
    public sealed class RecoveryCoordinator : IRecoveryCoordinator
    {
        private readonly IWorkflowPersistenceProvider _provider;
        private readonly RecoveryMiddlewareOptions _options;

        public RecoveryCoordinator(IWorkflowPersistenceProvider provider, RecoveryMiddlewareOptions? options = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _options = options ?? new RecoveryMiddlewareOptions();
        }

        public async Task ResumeAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            Guid foundryKey,
            Guid workflowKey,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await _provider.TryLoadAsync(foundryKey, workflowKey, cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
            {
                return; // nothing to recover
            }

            var foundry = foundryFactory();
            var workflow = workflowFactory();

            // Restore properties
            foreach (var kv in snapshot.Properties)
            {
                foundry.Properties[kv.Key] = kv.Value;
            }

            // Execute starting from NextOperationIndex (middleware will also skip as safety net)
            var smith = WorkflowForge.CreateSmith(foundry.Logger, foundry.ServiceProvider);
            foundry.SetCurrentWorkflow(workflow);

            // Minimal retry on resume failures per options
            int attempts = 0;
            Exception? lastEx = null;
            while (attempts < _options.MaxRetryAttempts)
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
                    if (attempts >= _options.MaxRetryAttempts) break;

                    var delay = _options.BaseDelay;
                    if (_options.UseExponentialBackoff)
                    {
                        var factor = Math.Pow(2, attempts - 1);
                        delay = TimeSpan.FromMilliseconds(_options.BaseDelay.TotalMilliseconds * factor);
                    }
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // Surface the last exception after exhausting retries
            if (lastEx != null) throw lastEx;
        }

        public async Task<int> ResumeAllAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            IRecoveryCatalog catalog,
            CancellationToken cancellationToken = default)
        {
            var snapshots = await catalog.ListPendingAsync(cancellationToken).ConfigureAwait(false);
            int success = 0;
            foreach (var s in snapshots)
            {
                try
                {
                    await ResumeAsync(foundryFactory, workflowFactory, s.FoundryExecutionId, s.WorkflowId, cancellationToken).ConfigureAwait(false);
                    success++;
                }
                catch
                {
                    // best-effort resume; continue others
                }
            }
            return success;
        }
    }
}