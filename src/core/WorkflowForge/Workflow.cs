using System;
using System.Collections.Generic;
using System.Linq;
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
        public bool SupportsRestore { get; }

        public Workflow(
            string name,
            string? description,
            string version,
            IList<IWorkflowOperation> operations,
            IDictionary<string, object?> properties)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Operations = new List<IWorkflowOperation>(operations ?? throw new ArgumentNullException(nameof(operations)));
            Properties = new Dictionary<string, object?>(properties ?? throw new ArgumentNullException(nameof(properties)));
            CreatedAt = DateTimeOffset.UtcNow;
            SupportsRestore = operations.All(op => op.SupportsRestore);
        }

        public void Dispose()
        {
            foreach (var operation in Operations)
            {
                operation?.Dispose();
            }
        }
    }
}