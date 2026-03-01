using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WorkflowForge.Abstractions;

namespace WorkflowForge
{
    /// <summary>
    /// Implementation of IWorkflow for built workflows.
    /// </summary>
    internal sealed class Workflow : IWorkflow
    {
        public Guid Id { get; }
        public string Name { get; }
        public string? Description { get; }
        public string Version { get; }
        public IReadOnlyList<IWorkflowOperation> Operations { get; }
        public IReadOnlyDictionary<string, object?> Properties { get; }
        public DateTimeOffset CreatedAt { get; }

        public Workflow(
            string name,
            string? description,
            string version,
            IList<IWorkflowOperation> operations,
            IDictionary<string, object?> properties,
            ISystemTimeProvider? timeProvider = null)
        {
            var time = timeProvider ?? SystemTimeProvider.Instance;
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Operations = new ReadOnlyCollection<IWorkflowOperation>(
                new List<IWorkflowOperation>(operations ?? throw new ArgumentNullException(nameof(operations))));
            Properties = new Dictionary<string, object?>(properties ?? throw new ArgumentNullException(nameof(properties)));
            CreatedAt = time.UtcNow;
        }

        public void Dispose()
        {
            foreach (var operation in Operations)
            {
                operation?.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}