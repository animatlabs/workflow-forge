using System;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.ExceptionsTests
{
    /// <summary>
    /// Tests for <see cref="WorkflowOperationException"/>.
    /// </summary>
    public class WorkflowOperationExceptionShould
    {
        #region Constructor Tests

        [Fact]
        public void SetAllProperties_GivenFullConstructor()
        {
            // Arrange
            const string message = "Operation failed during execution";
            var innerException = new InvalidOperationException("Inner error");
            var executionId = Guid.NewGuid();
            var workflowId = Guid.NewGuid();
            const string operationName = "ProcessOrder";
            var operationId = Guid.NewGuid();

            // Act
            var exception = new WorkflowOperationException(
                message,
                innerException,
                executionId,
                workflowId,
                operationName,
                operationId);

            // Assert
            Assert.Contains(message, exception.Message);
            Assert.Contains("ExecutionId=", exception.Message);
            Assert.Contains("WorkflowId=", exception.Message);
            Assert.Contains("Operation=" + operationName, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(executionId, exception.ExecutionId);
            Assert.Equal(workflowId, exception.WorkflowId);
            Assert.Equal(operationName, exception.OperationName);
            Assert.Equal(operationId, exception.OperationId);
        }

        #endregion Constructor Tests
    }
}
