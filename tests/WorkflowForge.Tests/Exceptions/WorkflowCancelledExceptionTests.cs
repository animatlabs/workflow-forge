using System;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.Exceptions
{
    /// <summary>
    /// Tests for <see cref="WorkflowCancelledException"/>.
    /// </summary>
    public class WorkflowCancelledExceptionShould
    {
        #region Constructor Tests

        [Fact]
        public void SetMessage_GivenMessage()
        {
            // Arrange
            const string message = "Workflow was cancelled by user";

            // Act
            var exception = new WorkflowCancelledException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void SetEmptyMessage_GivenEmptyMessage()
        {
            // Act
            var exception = new WorkflowCancelledException(string.Empty);

            // Assert
            Assert.Equal(string.Empty, exception.Message);
        }

        [Fact]
        public void SetBoth_GivenMessageAndInnerException()
        {
            // Arrange
            const string message = "Workflow was cancelled";
            var innerException = new OperationCanceledException("Token cancelled");

            // Act
            var exception = new WorkflowCancelledException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void BeAssignableToWorkflowForgeException_GivenConstruction()
        {
            // Act
            var exception = new WorkflowCancelledException("Cancelled");

            // Assert
            Assert.IsAssignableFrom<WorkflowForgeException>(exception);
        }

        #endregion Constructor Tests
    }
}
