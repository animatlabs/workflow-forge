using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WorkflowForge.Tests.Orchestration;

/// <summary>
/// Tests for WorkflowSmith Dispose behavior, including event handler cleanup (memory leak prevention).
/// </summary>
public class WorkflowSmithDisposeShould
{
    [Fact]
    public void ClearEventHandlers_GivenSubscribedEventHandlers()
    {
        // Arrange
        var smith = WorkflowForge.CreateSmith();
        smith.WorkflowStarted += (s, e) => { };
        smith.WorkflowCompleted += (s, e) => { };
        smith.WorkflowFailed += (s, e) => { };
        smith.CompensationTriggered += (s, e) => { };
        smith.CompensationCompleted += (s, e) => { };
        smith.OperationRestoreStarted += (s, e) => { };
        smith.OperationRestoreCompleted += (s, e) => { };
        smith.OperationRestoreFailed += (s, e) => { };

        // Act
        smith.Dispose();

        // Assert
        var smithType = smith.GetType();
        var eventNames = new[] { "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed", "CompensationTriggered",
            "CompensationCompleted", "OperationRestoreStarted", "OperationRestoreCompleted", "OperationRestoreFailed" };

        foreach (var eventName in eventNames)
        {
            var field = smithType.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.True(field != null, $"Expected backing field '{eventName}' not found on {smithType.Name}");
            var value = field.GetValue(smith);
            Assert.True(value == null, $"Event {eventName} backing field should be null after Dispose");
        }
    }

    [Fact]
    public async Task ThrowObjectDisposedException_GivenSubsequentForgeAsyncCall()
    {
        // Arrange
        var smith = WorkflowForge.CreateSmith();
        var workflow = WorkflowForge.CreateWorkflow("Test")
            .AddOperation("Op1", (foundry, ct) => Task.CompletedTask)
            .Build();

        smith.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => smith.ForgeAsync(workflow));
    }

    [Fact]
    public void NotThrow_GivenMultipleDisposeCalls()
    {
        // Arrange
        var smith = WorkflowForge.CreateSmith();

        // Act & Assert
        var ex = Record.Exception(() =>
        {
            smith.Dispose();
            smith.Dispose();
            smith.Dispose();
        });
        Assert.Null(ex);
    }
}
