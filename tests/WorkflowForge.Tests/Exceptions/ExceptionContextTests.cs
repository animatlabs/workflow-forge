using System;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.Exceptions
{
    /// <summary>
    /// Tests for exception context properties (ExecutionId, WorkflowId, OperationName).
    /// </summary>
    public class ExceptionContextTests
    {
        #region WorkflowOperationException Tests

        [Fact]
        public void WorkflowOperationException_WithContext_IncludesInMessage()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var workflowId = Guid.NewGuid();
            var operationName = "TestOperation";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new WorkflowOperationException(
                "Operation failed",
                innerException,
                executionId,
                workflowId,
                operationName);

            // Assert
            Assert.Contains($"ExecutionId={executionId}", exception.Message);
            Assert.Contains($"WorkflowId={workflowId}", exception.Message);
            Assert.Contains($"Operation={operationName}", exception.Message);
            Assert.Equal(executionId, exception.ExecutionId);
            Assert.Equal(workflowId, exception.WorkflowId);
            Assert.Equal(operationName, exception.OperationName);
        }

        [Fact]
        public void WorkflowOperationException_WithNullContext_NoContextInMessage()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new WorkflowOperationException(
                "Operation failed",
                innerException,
                null,
                null,
                null);

            // Assert
            Assert.Equal("Operation failed", exception.Message);
            Assert.Null(exception.ExecutionId);
            Assert.Null(exception.WorkflowId);
            Assert.Null(exception.OperationName);
        }

        [Fact]
        public void WorkflowOperationException_LegacyConstructor_StillWorks()
        {
            // Arrange & Act
            var exception = new WorkflowOperationException(
                "Operation failed",
                "TestOperation",
                Guid.NewGuid());

            // Assert
            Assert.Contains("Operation failed", exception.Message);
            Assert.Equal("TestOperation", exception.OperationName);
        }

        [Fact]
        public void WorkflowOperationException_WithPartialContext_OnlyIncludesProvided()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new WorkflowOperationException(
                "Operation failed",
                innerException,
                executionId,
                null,
                "TestOp");

            // Assert
            Assert.Contains($"ExecutionId={executionId}", exception.Message);
            Assert.DoesNotContain("WorkflowId=", exception.Message);
            Assert.Contains("Operation=TestOp", exception.Message);
        }

        #endregion WorkflowOperationException Tests

        #region WorkflowRestoreException Tests

        [Fact]
        public void WorkflowRestoreException_WithContext_IncludesInMessage()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var workflowId = Guid.NewGuid();
            var operationName = "TestRestoreOp";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new WorkflowRestoreException(
                "Restore failed",
                innerException,
                executionId,
                workflowId,
                operationName);

            // Assert
            Assert.Contains($"ExecutionId={executionId}", exception.Message);
            Assert.Contains($"WorkflowId={workflowId}", exception.Message);
            Assert.Contains($"Operation={operationName}", exception.Message);
            Assert.Equal(executionId, exception.ExecutionId);
            Assert.Equal(workflowId, exception.WorkflowId);
            Assert.Equal(operationName, exception.OperationName);
        }

        [Fact]
        public void WorkflowRestoreException_LegacyConstructor_StillWorks()
        {
            // Arrange & Act
            var exception = new WorkflowRestoreException("Restore failed");

            // Assert
            Assert.Equal("Restore failed", exception.Message);
        }

        [Fact]
        public void WorkflowRestoreException_LegacyWithInner_StillWorks()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new WorkflowRestoreException("Restore failed", innerException);

            // Assert
            Assert.Equal("Restore failed", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        #endregion WorkflowRestoreException Tests
    }
}