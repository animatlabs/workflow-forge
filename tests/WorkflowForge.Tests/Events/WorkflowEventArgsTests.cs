using System;
using System.Collections.Generic;
using WorkflowForge.Events;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Events
{
    /// <summary>
    /// Tests for workflow-level event argument classes.
    /// </summary>
    public class WorkflowEventArgsTests
    {
        #region WorkflowStartedEventArgs Tests

        [Fact]
        public void WorkflowStartedEventArgs_Constructor_SetsAllProperties()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

            // Act
            var args = new WorkflowStartedEventArgs(foundry, startedAt);

            // Assert
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(startedAt, args.Timestamp);
            Assert.Equal(startedAt, args.StartedAt);
        }

        #endregion WorkflowStartedEventArgs Tests

        #region WorkflowCompletedEventArgs Tests

        [Fact]
        public void WorkflowCompletedEventArgs_Constructor_SetsAllProperties()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var completedAt = DateTimeOffset.UtcNow;
            var finalProperties = new Dictionary<string, object?>
            {
                { "result", "success" },
                { "count", 42 }
            };
            var duration = TimeSpan.FromSeconds(10);

            // Act
            var args = new WorkflowCompletedEventArgs(
                foundry, completedAt, finalProperties, duration);

            // Assert
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(completedAt, args.Timestamp);
            Assert.Equal(completedAt, args.CompletedAt);
            Assert.Same(finalProperties, args.FinalProperties);
            Assert.Equal(2, args.FinalProperties.Count);
            Assert.Equal("success", args.FinalProperties["result"]);
            Assert.Equal(42, args.FinalProperties["count"]);
            Assert.Equal(duration, args.Duration);
        }

        [Fact]
        public void WorkflowCompletedEventArgs_Constructor_WithEmptyProperties_SetsCorrectly()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var completedAt = DateTimeOffset.UtcNow;
            var finalProperties = new Dictionary<string, object?>();

            // Act
            var args = new WorkflowCompletedEventArgs(
                foundry, completedAt, finalProperties, TimeSpan.Zero);

            // Assert
            Assert.NotNull(args.FinalProperties);
            Assert.Empty(args.FinalProperties);
        }

        [Fact]
        public void WorkflowCompletedEventArgs_Constructor_WithNullFinalProperties_ThrowsArgumentNullException()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var completedAt = DateTimeOffset.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WorkflowCompletedEventArgs(foundry, completedAt, null!, TimeSpan.Zero));
        }

        #endregion WorkflowCompletedEventArgs Tests
    }
}
