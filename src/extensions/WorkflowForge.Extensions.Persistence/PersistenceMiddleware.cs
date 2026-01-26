using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Persists and resumes workflow execution by checkpointing after each operation via a user-provided provider.
    /// </summary>
    public sealed class PersistenceMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowPersistenceProvider _provider;
        private readonly PersistenceOptions? _options;

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
            if (foundry.CurrentWorkflow is { } wf)
            {
                var (foundryKey, workflowKey) = ResolveKeys(foundry, wf);
                var snapshot = await _provider.TryLoadAsync(foundryKey, workflowKey, cancellationToken).ConfigureAwait(false);
                if (snapshot != null)
                {
                    const string restoredFlagKey = "__wf_persistence_restored__";
                    if (!foundry.Properties.ContainsKey(restoredFlagKey))
                    {
                        foreach (var kv in snapshot.Properties)
                        {
                            foundry.Properties[kv.Key] = kv.Value;
                        }
                        foundry.Properties[restoredFlagKey] = true;
                    }

                    var operations = wf.Operations;
                    var currentIndex = operations.ToList().FindIndex(op => op.Id == operation.Id);
                    if (snapshot.NextOperationIndex > currentIndex)
                    {
                        return inputData;
                    }
                }
            }

            var result = await next(cancellationToken).ConfigureAwait(false);

            if (foundry.CurrentWorkflow is { } workflow)
            {
                var operations = workflow.Operations;
                var index = operations.ToList().FindIndex(op => op.Id == operation.Id);
                var snapshot = new WorkflowExecutionSnapshot
                {
                    FoundryExecutionId = ResolveKeys(foundry, workflow).foundryKey,
                    WorkflowId = ResolveKeys(foundry, workflow).workflowKey,
                    WorkflowName = workflow.Name,
                    NextOperationIndex = index + 1,
                    Properties = new Dictionary<string, object?>(foundry.Properties)
                };

                await _provider.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);

                if (snapshot.NextOperationIndex >= operations.Count)
                {
                    var (foundryKey, workflowKey) = ResolveKeys(foundry, workflow);
                    await _provider.DeleteAsync(foundryKey, workflowKey, cancellationToken).ConfigureAwait(false);
                }
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