using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WorkflowForge.Tests.Orchestration;

/// <summary>
/// Tests for WorkflowSmith Dispose behavior, including event handler cleanup (memory leak prevention).
/// </summary>
public class WorkflowSmithDisposeTests
{
    [Fact]
    public void Dispose_GivenSubscribedEventHandlers_ClearsEventHandlers()
    {
        // Arrange - Create smith via public API and subscribe to events
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

        // Assert - Use reflection to verify event backing fields are null (handlers cleared)
        var smithType = smith.GetType();
        var eventNames = new[] { "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed", "CompensationTriggered",
            "CompensationCompleted", "OperationRestoreStarted", "OperationRestoreCompleted", "OperationRestoreFailed" };

        foreach (var eventName in eventNames)
        {
            // Compiler-generated backing field: <EventName>k__BackingField
            var backingFieldName = $"<{eventName}>k__BackingField";
            var field = smithType.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var value = field.GetValue(smith);
                Assert.True(value == null, $"Event {eventName} backing field should be null after Dispose");
            }
        }
    }

    [Fact]
    public async Task Dispose_WhenCalled_SubsequentForgeAsyncThrowsObjectDisposedException()
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
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var smith = WorkflowForge.CreateSmith();

        // Act & Assert
        smith.Dispose();
        smith.Dispose();
        smith.Dispose();
    }
}