using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.OrchestrationTests;

/// <summary>
/// Subscribes to the smith lifecycle events and asserts they actually fire, including the
/// compensation and restore events that no other test observes.
/// </summary>
public class WorkflowSmithEventsShould
{
    [Fact]
    public async Task RaiseStartedAndCompleted_GivenASuccessfulWorkflow()
    {
        var raised = new List<string>();
        using var smith = WorkflowForge.CreateSmith();
        smith.WorkflowStarted += (_, _) => raised.Add("started");
        smith.WorkflowCompleted += (_, _) => raised.Add("completed");
        smith.WorkflowFailed += (_, _) => raised.Add("failed");

        var workflow = WorkflowForge.CreateWorkflow("EventsSuccess")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .Build();

        await smith.ForgeAsync(workflow);

        Assert.Equal(new[] { "started", "completed" }, raised.ToArray());
    }

    [Fact]
    public async Task RaiseTheCompensationEvents_GivenAFailureAfterARestorableOperation()
    {
        var raised = new List<string>();
        using var smith = WorkflowForge.CreateSmith();
        smith.WorkflowFailed += (_, _) => raised.Add("workflowFailed");
        smith.CompensationTriggered += (_, _) => raised.Add("compensationTriggered");
        smith.OperationRestoreStarted += (_, e) => raised.Add("restoreStarted:" + e.Operation.Name);
        smith.OperationRestoreCompleted += (_, e) => raised.Add("restoreCompleted:" + e.Operation.Name);
        smith.CompensationCompleted += (_, e) => raised.Add("compensationCompleted:" + e.SuccessCount + "/" + e.FailureCount);

        var workflow = WorkflowForge.CreateWorkflow("EventsCompensate")
            .AddOperation(new RestorableOperation("first"))
            .AddOperation("boom", (_, _) => throw new InvalidOperationException("fail"))
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow));

        Assert.Contains("workflowFailed", raised);
        Assert.Contains("compensationTriggered", raised);
        Assert.Contains("restoreStarted:first", raised);
        Assert.Contains("restoreCompleted:first", raised);
        Assert.Contains(raised, r => r.StartsWith("compensationCompleted:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RaiseOperationRestoreFailed_GivenRestoreThrows()
    {
        var raised = new List<string>();
        using var smith = WorkflowForge.CreateSmith();
        smith.OperationRestoreFailed += (_, e) => raised.Add("restoreFailed:" + e.Operation.Name);

        var workflow = WorkflowForge.CreateWorkflow("EventsRestoreFail")
            .AddOperation(new RestorableOperation("first", throwOnRestore: true))
            .AddOperation("boom", (_, _) => throw new InvalidOperationException("fail"))
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow));

        Assert.Contains("restoreFailed:first", raised);
    }

    private sealed class RestorableOperation : IWorkflowOperation
    {
        private readonly bool _throwOnRestore;

        public RestorableOperation(string name, bool throwOnRestore = false)
        {
            Name = name;
            _throwOnRestore = throwOnRestore;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; }

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult(inputData);

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_throwOnRestore)
                throw new InvalidOperationException("restore failed");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
