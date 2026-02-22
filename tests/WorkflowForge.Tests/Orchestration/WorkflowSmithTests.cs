using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Options;

namespace WorkflowForge.Tests.Orchestration
{
    public class WorkflowSmithTests
    {
        [Fact]
        public async Task ForgeAsync_WhenOperationFails_EmitsFailedOperationName()
        {
            // Arrange
            using var smith = global::WorkflowForge.WorkflowForge.CreateSmith();
            var operation1 = CreateMockOperation("Op1", result: "result1");
            var operation2 = CreateMockOperation("Op2", exception: new InvalidOperationException("boom"));

            var workflow = new Workflow(
                "TestWorkflow",
                "Test Description",
                "1.0.0",
                new List<IWorkflowOperation> { operation1.Object, operation2.Object },
                new Dictionary<string, object?>());

            string? failedOperationName = null;
            smith.WorkflowFailed += (_, args) => failedOperationName = args.FailedOperationName;

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => smith.ForgeAsync(workflow));

            // Assert
            Assert.Equal("Op2", failedOperationName);
        }

        [Fact]
        public async Task ForgeAsync_WhenCompensating_PassesStoredOutputToRestore()
        {
            // Arrange
            using var smith = global::WorkflowForge.WorkflowForge.CreateSmith();
            var operation1 = CreateMockOperation("Op1", result: "result1");
            var operation2 = CreateMockOperation("Op2", exception: new InvalidOperationException("boom"));

            var workflow = new Workflow(
                "CompensationWorkflow",
                "Test Description",
                "1.0.0",
                new List<IWorkflowOperation> { operation1.Object, operation2.Object },
                new Dictionary<string, object?>());

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => smith.ForgeAsync(workflow));

            // Assert
            operation1.Verify(op => op.RestoreAsync("result1", It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgeAsync_WhenCompensationFails_AggregatesErrors()
        {
            // Arrange
            var options = new WorkflowForgeOptions
            {
                ThrowOnCompensationError = true
            };
            using var smith = global::WorkflowForge.WorkflowForge.CreateSmith(options: options);
            var operation1 = CreateMockOperation("Op1", result: "result1", restoreException: new InvalidOperationException("restore-failed"));
            var operation2 = CreateMockOperation("Op2", exception: new InvalidOperationException("boom"));

            var workflow = new Workflow(
                "CompensationWorkflow",
                "Test Description",
                "1.0.0",
                new List<IWorkflowOperation> { operation1.Object, operation2.Object },
                new Dictionary<string, object?>());

            // Act
            var exception = await Assert.ThrowsAsync<AggregateException>(() => smith.ForgeAsync(workflow));

            // Assert
            Assert.Contains(exception.InnerExceptions, ex => ex is InvalidOperationException && ex.Message == "boom");
            Assert.Contains(exception.InnerExceptions, ex => ex is InvalidOperationException && ex.Message == "restore-failed");
        }

        [Fact]
        public async Task ForgeAsync_WhenFoundryProvided_ReplacesOperations()
        {
            // Arrange
            using var smith = global::WorkflowForge.WorkflowForge.CreateSmith();
            var operation = CreateMockOperation("Op1", result: "result1");
            var workflow = new Workflow(
                "TestWorkflow",
                "Test Description",
                "1.0.0",
                new List<IWorkflowOperation> { operation.Object },
                new Dictionary<string, object?>());

            var foundry = new Mock<IWorkflowFoundry>();
            foundry.Setup(f => f.SetCurrentWorkflow(workflow));
            foundry.Setup(f => f.ReplaceOperations(It.IsAny<IEnumerable<IWorkflowOperation>>()));
            foundry.Setup(f => f.ForgeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            foundry.SetupGet(f => f.ExecutionId).Returns(Guid.NewGuid());
            foundry.SetupGet(f => f.Properties).Returns(new ConcurrentDictionary<string, object?>());
            foundry.SetupGet(f => f.Logger).Returns(NullLogger.Instance);
            foundry.SetupGet(f => f.ServiceProvider).Returns((IServiceProvider?)null);
            foundry.SetupGet(f => f.CurrentWorkflow).Returns(workflow);
            foundry.SetupGet(f => f.IsFrozen).Returns(false);

            // Act
            await smith.ForgeAsync(workflow, foundry.Object);

            // Assert
            foundry.Verify(f => f.ReplaceOperations(workflow.Operations), Times.Once);
            foundry.Verify(f => f.ForgeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Mock<IWorkflowOperation> CreateMockOperation(
            string name,
            string? result = null,
            Exception? exception = null,
            Exception? restoreException = null)
        {
            var mock = new Mock<IWorkflowOperation>();
            mock.SetupGet(op => op.Id).Returns(Guid.NewGuid());
            mock.SetupGet(op => op.Name).Returns(name);

            if (exception != null)
            {
                mock.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);
            }
            else
            {
                mock.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(result);
            }

            if (restoreException != null)
            {
                mock.Setup(op => op.RestoreAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(restoreException);
            }
            else
            {
                mock.Setup(op => op.RestoreAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            }

            return mock;
        }
    }
}
