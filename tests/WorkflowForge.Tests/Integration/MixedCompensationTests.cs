using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Integration;

/// <summary>
/// Integration tests for mixed compensation scenarios: LoggingOperation (no-op RestoreAsync)
/// plus restorable operations plus failing operations.
/// </summary>
public class MixedCompensationTests
{
    [Fact]
    public async Task ForgeAsync_GivenLoggingPlusRestorablePlusFailing_TriggersCompensation_RestorableOpRestored_LoggingOpNoOpRestoreDoesNotThrow()
    {
        // Arrange: LoggingOperation (no-op RestoreAsync) + restorable op + failing op
        var restorableRestoreInvoked = false;
        var workflow = WorkflowForge.CreateWorkflow("MixedCompensation")
            .AddOperation(LoggingOperation.Info("Step1"))
            .AddOperation("RestorableStep", (foundry, ct) => Task.CompletedTask,
                (foundry, ct) =>
                {
                    restorableRestoreInvoked = true;
                    return Task.CompletedTask;
                })
            .AddOperation("FailingStep", (foundry, ct) =>
            {
                throw new InvalidOperationException("Intentional failure");
            })
            .Build();

        using var smith = WorkflowForge.CreateSmith();
        var foundry = WorkflowForge.CreateFoundry("MixedCompensation");

        // Act - Workflow fails, compensation runs (ActionWorkflowOperation wraps in WorkflowOperationException)
        await Assert.ThrowsAsync<WorkflowOperationException>(() => smith.ForgeAsync(workflow, foundry));

        // Assert - Restorable op's restore delegate was invoked; LoggingOperation's no-op RestoreAsync did not throw
        Assert.True(restorableRestoreInvoked);
    }

    [Fact]
    public async Task ForgeAsync_GivenRestorableThenLoggingThenFailing_CompensatesInReverseOrder_LoggingRestoreSucceeds()
    {
        // Arrange: Restorable first, then LoggingOperation, then failing
        var restoreOrder = new List<string>();
        var workflow = WorkflowForge.CreateWorkflow("ReverseCompensation")
            .AddOperation("RestorableFirst", (foundry, ct) => Task.CompletedTask,
                (foundry, ct) =>
                {
                    restoreOrder.Add("RestorableFirst");
                    return Task.CompletedTask;
                })
            .AddOperation(LoggingOperation.Info("LogStep"))
            .AddOperation("FailingStep", (foundry, ct) =>
            {
                throw new InvalidOperationException("Fail");
            })
            .Build();

        using var smith = WorkflowForge.CreateSmith();
        var foundry = WorkflowForge.CreateFoundry("ReverseCompensation");

        // Act - ActionWorkflowOperation wraps in WorkflowOperationException
        await Assert.ThrowsAsync<WorkflowOperationException>(() => smith.ForgeAsync(workflow, foundry));

        // Assert - Compensation runs in reverse: FailingStep (skipped), LoggingStep (no-op), RestorableFirst (restored)
        Assert.Contains("RestorableFirst", restoreOrder);
        Assert.Single(restoreOrder); // Only RestorableFirst has real restore logic; LoggingOperation no-op doesn't add to list
    }
}