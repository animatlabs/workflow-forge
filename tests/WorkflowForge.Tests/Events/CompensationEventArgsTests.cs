using System;
using WorkflowForge.Events;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Events
{
    /// <summary>
    /// Tests for compensation event argument classes.
    /// </summary>
    public class CompensationEventArgsShould
    {
        #region CompensationTriggeredEventArgs Tests

        [Fact]
        public void SetAllProperties_GivenCompensationTriggeredEventArgsConstruction()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var triggeredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            const string reason = "Operation X failed";
            const string failedOperationName = "OperationX";
            var exception = new InvalidOperationException("Test failure");

            // Act
            var args = new CompensationTriggeredEventArgs(
                foundry, triggeredAt, reason, failedOperationName, exception);

            // Assert
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(triggeredAt, args.Timestamp);
            Assert.Equal(triggeredAt, args.TriggeredAt);
            Assert.Equal(reason, args.Reason);
            Assert.Equal(failedOperationName, args.FailedOperationName);
            Assert.Same(exception, args.Exception);
        }

        [Fact]
        public void SetEmptyString_GivenNullReason()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var triggeredAt = DateTimeOffset.UtcNow;

            // Act
            var args = new CompensationTriggeredEventArgs(
                foundry, triggeredAt, null!, "OpName", null);

            // Assert
            Assert.Equal(string.Empty, args.Reason);
        }

        [Fact]
        public void SetEmptyString_GivenNullFailedOperationName()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var triggeredAt = DateTimeOffset.UtcNow;

            // Act
            var args = new CompensationTriggeredEventArgs(
                foundry, triggeredAt, "reason", null!, null);

            // Assert
            Assert.Equal(string.Empty, args.FailedOperationName);
        }

        [Fact]
        public void SetNullException_GivenNullException()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var triggeredAt = DateTimeOffset.UtcNow;

            // Act
            var args = new CompensationTriggeredEventArgs(
                foundry, triggeredAt, "reason", "OpName", null);

            // Assert
            Assert.Null(args.Exception);
        }

        #endregion CompensationTriggeredEventArgs Tests

        #region CompensationCompletedEventArgs Tests

        [Fact]
        public void SetAllProperties_GivenCompensationCompletedEventArgsConstruction()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var completedAt = DateTimeOffset.UtcNow;
            const int successCount = 3;
            const int failureCount = 1;
            var duration = TimeSpan.FromSeconds(2.5);

            // Act
            var args = new CompensationCompletedEventArgs(
                foundry, completedAt, successCount, failureCount, duration);

            // Assert
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(completedAt, args.Timestamp);
            Assert.Equal(completedAt, args.CompletedAt);
            Assert.Equal(successCount, args.SuccessCount);
            Assert.Equal(failureCount, args.FailureCount);
            Assert.Equal(duration, args.Duration);
        }

        [Fact]
        public void SetCorrectly_GivenZeroCounts()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var completedAt = DateTimeOffset.UtcNow;

            // Act
            var args = new CompensationCompletedEventArgs(
                foundry, completedAt, 0, 0, TimeSpan.Zero);

            // Assert
            Assert.Equal(0, args.SuccessCount);
            Assert.Equal(0, args.FailureCount);
        }

        #endregion CompensationCompletedEventArgs Tests
    }
}
