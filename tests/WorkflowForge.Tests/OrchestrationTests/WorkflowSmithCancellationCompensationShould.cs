using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.OrchestrationTests;

public class WorkflowSmithCancellationCompensationShould
{
    [Fact]
    public async Task NotRunCompensation_GivenForwardExecutionCancelledAfterAnOperationCompleted()
    {
        using var smith = WorkflowForge.CreateSmith();
        var step1Ran = false;
        var compensationRan = false;

        using var cts = new CancellationTokenSource();

        var workflow = WorkflowForge.CreateWorkflow("CancelForward")
            .AddOperation(new TrackingOperation(
                "Step1",
                onRestore: () => compensationRan = true,
                onForge: () =>
                {
                    step1Ran = true;
                    cts.Cancel();
                }))
            .AddOperation(new DelegateWorkflowOperation("Step2", (_, _, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<object?>(null);
            }))
            .Build();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            smith.ForgeAsync(workflow, cts.Token));

        // Step1 completed, so there was something to compensate; forward cancellation must still skip it.
        Assert.True(step1Ran);
        Assert.False(compensationRan);
    }

    [Fact]
    public async Task RecordCompensationFailure_GivenCancellationDuringRestoreAfterWorkflowFailure()
    {
        var logger = new RecordingLogger();
        using var smith = WorkflowForge.CreateSmith(logger);
        var restoreAttempts = 0;

        var workflow = WorkflowForge.CreateWorkflow("CancelDuringRestore")
            .AddOperation(new TrackingOperation("Step1", onRestore: () =>
            {
                restoreAttempts++;
                throw new OperationCanceledException();
            }))
            .AddOperation(new DelegateWorkflowOperation("Fail", (_, _, _) =>
                Task.FromException<object?>(new InvalidOperationException("forward failure"))))
            .Build();

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() => smith.ForgeAsync(workflow));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal(1, restoreAttempts);

        // The compensation failure is swallowed by default, so the log is the only record of it.
        Assert.Contains(
            logger.Entries,
            e => e.Exception is OperationCanceledException);
    }

    private sealed class TrackingOperation : WorkflowOperationBase
    {
        private readonly Action _onRestore;
        private readonly Action? _onForge;

        public TrackingOperation(string name, Action onRestore, Action? onForge = null)
        {
            Name = name;
            _onRestore = onRestore;
            _onForge = onForge;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            _onForge?.Invoke();
            return Task.FromResult<object?>(null);
        }

        public override Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            _onRestore();
            return Task.CompletedTask;
        }
    }
}
