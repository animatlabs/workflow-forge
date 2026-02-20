using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Persists and resumes workflow execution by checkpointing after each operation via a user-provided provider.
    /// Uses an index-based counter to track the current operation position. The counter is stored on
    /// <c>foundry.Properties</c> and incremented after each operation completes, making it O(1) per operation
    /// with no dictionary lookups. Safe for nested workflows because each foundry has its own property dictionary.
    /// </summary>
    public sealed class PersistenceMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowPersistenceProvider _provider;
        private readonly PersistenceOptions? _options;

        /// <summary>
        /// Internal property key for the execution counter used to track current operation index.
        /// </summary>
        internal const string ExecutionCounterKey = "__wf_persistence_exec_counter__";

        /// <summary>
        /// Internal property key used to flag that snapshot state has been restored.
        /// </summary>
        internal const string RestoredFlagKey = "__wf_persistence_restored__";

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceMiddleware"/> class.
        /// </summary>
        /// <param name="provider">The persistence provider that saves, loads, and deletes workflow snapshots.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
        public PersistenceMiddleware(IWorkflowPersistenceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceMiddleware"/> class with options for stable keys.
        /// </summary>
        /// <param name="provider">The persistence provider.</param>
        /// <param name="options">Options controlling stable keys for cross-process resume.</param>
        public PersistenceMiddleware(IWorkflowPersistenceProvider provider, PersistenceOptions options)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Executes an operation within the persistence pipeline. Restores state when available,
        /// skips operations that were already completed, and checkpoints after successful execution.
        /// The current operation index is read from <see cref="FoundryPropertyKeys.CurrentOperationIndex"/>
        /// which the foundry sets before each middleware invocation.
        /// </summary>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="foundry">The foundry execution context.</param>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="next">Delegate to invoke the next middleware/operation.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The operation result or the passthrough input when the step is skipped.</returns>
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var workflow = foundry.CurrentWorkflow;
            if (workflow == null)
            {
                return await next(cancellationToken).ConfigureAwait(false);
            }

            // Read current operation index from foundry (set by WorkflowFoundry before middleware invocation)
            int currentIndex;
            if (foundry.Properties.TryGetValue(FoundryPropertyKeys.CurrentOperationIndex, out var indexObj) && indexObj is int idx)
            {
                currentIndex = idx;
            }
            else
            {
                // Fallback: use an internal counter if the foundry didn't set the index
                // (e.g., older foundry versions or custom foundry implementations)
                if (foundry.Properties.TryGetValue(ExecutionCounterKey, out var counterObj) && counterObj is int counter)
                {
                    currentIndex = counter;
                }
                else
                {
                    currentIndex = 0;
                }

                foundry.Properties[ExecutionCounterKey] = currentIndex + 1;
            }

            // Resolve stable keys once
            var (foundryKey, workflowKey) = ResolveKeys(foundry, workflow);

            // Restore + skip logic
            var snapshot = await _provider.TryLoadAsync(foundryKey, workflowKey, cancellationToken).ConfigureAwait(false);
            if (snapshot != null)
            {
                if (!foundry.Properties.ContainsKey(RestoredFlagKey))
                {
                    foreach (var kv in snapshot.Properties)
                    {
                        foundry.Properties[kv.Key] = kv.Value;
                    }
                    foundry.Properties[RestoredFlagKey] = true;
                }

                if (snapshot.NextOperationIndex > currentIndex)
                {
                    // Return the stored output from the restored snapshot so the foundry
                    // preserves the correct value for output chaining and compensation.
                    var outputKey = string.Format(FoundryPropertyKeys.OperationOutputFormat, currentIndex, operation.Name);
                    if (foundry.Properties.TryGetValue(outputKey, out var storedOutput))
                    {
                        return storedOutput;
                    }

                    return inputData;
                }
            }

            var result = await next(cancellationToken).ConfigureAwait(false);

            // Checkpoint after successful execution
            var operationCount = workflow.Operations.Count;
            var newSnapshot = new WorkflowExecutionSnapshot
            {
                FoundryExecutionId = foundryKey,
                WorkflowId = workflowKey,
                WorkflowName = workflow.Name,
                NextOperationIndex = currentIndex + 1,
                Properties = new Dictionary<string, object?>(foundry.Properties)
            };

            await _provider.SaveAsync(newSnapshot, cancellationToken).ConfigureAwait(false);

            // Clean up snapshot when all operations are done
            if (newSnapshot.NextOperationIndex >= operationCount)
            {
                await _provider.DeleteAsync(foundryKey, workflowKey, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private (Guid foundryKey, Guid workflowKey) ResolveKeys(IWorkflowFoundry foundry, IWorkflow workflow)
        {
            if (_options == null)
            {
                return (foundry.ExecutionId, workflow.Id);
            }

            var foundryKey = string.IsNullOrWhiteSpace(_options.InstanceId)
                ? foundry.ExecutionId
                : DeterministicGuid(_options.InstanceId!);

            var workflowKey = string.IsNullOrWhiteSpace(_options.WorkflowKey)
                ? workflow.Id
                : DeterministicGuid(_options.WorkflowKey!);

            return (foundryKey, workflowKey);
        }

        private static Guid DeterministicGuid(string input)
        {
            // Stable GUID derived from SHA1 hash of input
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha1.ComputeHash(bytes);
            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            return new Guid(guidBytes);
        }
    }
}