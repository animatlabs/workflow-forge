using System;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.ExceptionsTests
{
    /// <summary>
    /// Tests for <see cref="WorkflowRestoreException"/>.
    /// </summary>
    public class WorkflowRestoreExceptionShould
    {
        #region Constructor Tests

        [Fact]
        public void SetAllProperties_GivenFullConstructor()
        {
            // Arrange
            const string message = "Restoration failed during compensation";
            var innerException = new InvalidOperationException("Inner error");
            var executionId = Guid.NewGuid();
            var workflowId = Guid.NewGuid();
            const string operationName = "RestoreOrder";

            // Act
            var exception = new WorkflowRestoreException(
                message,
                innerException,
                executionId,
                workflowId,
                operationName);

            // Assert
            Assert.Contains(message, exception.Message);
            Assert.Contains("ExecutionId=", exception.Message);
            Assert.Contains("WorkflowId=", exception.Message);
            Assert.Contains("Operation=" + operationName, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(executionId, exception.ExecutionId);
            Assert.Equal(workflowId, exception.WorkflowId);
            Assert.Equal(operationName, exception.OperationName);
        }

        #endregion Constructor Tests
    }
}
