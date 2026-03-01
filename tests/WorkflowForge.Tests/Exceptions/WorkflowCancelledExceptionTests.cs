using System;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.Exceptions
{
    /// <summary>
    /// Tests for <see cref="WorkflowCancelledException"/>.
    /// </summary>
    public class WorkflowCancelledExceptionTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithMessage_SetsMessage()
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
        public void Constructor_WithEmptyMessage_SetsEmptyMessage()
        {
            // Act
            var exception = new WorkflowCancelledException(string.Empty);

            // Assert
            Assert.Equal(string.Empty, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
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
        public void Constructor_IsAssignableToWorkflowForgeException()
        {
            // Act
            var exception = new WorkflowCancelledException("Cancelled");

            // Assert
            Assert.IsAssignableFrom<WorkflowForgeException>(exception);
        }

        #endregion Constructor Tests
    }
}
