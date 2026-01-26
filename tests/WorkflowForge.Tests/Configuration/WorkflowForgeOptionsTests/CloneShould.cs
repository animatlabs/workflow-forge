using System;
using WorkflowForge.Options;

namespace WorkflowForge.Tests.Configuration.WorkflowForgeOptionsTests
{
    /// <summary>
    /// Tests for WorkflowForgeOptions.Clone() method and ICloneable implementation
    /// </summary>
    public class CloneShould
    {
        [Fact]
        public void ImplementICloneable()
        {
            // Arrange
            var options = new WorkflowForgeOptions();

            // Assert
            Assert.IsAssignableFrom<ICloneable>(options);
        }

        [Fact]
        public void CreateIndependentCopy_GivenDefaultConfiguration()
        {
            // Arrange
            var original = new WorkflowForgeOptions();

            // Act - ICloneable.Clone() returns object
            var clone = (WorkflowForgeOptions)original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.MaxConcurrentWorkflows, clone.MaxConcurrentWorkflows);
        }

        [Fact]
        public void CreateIndependentCopy_GivenCustomConfiguration()
        {
            // Arrange
            var original = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 50
            };

            // Act
            var clone = (WorkflowForgeOptions)original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(50, clone.MaxConcurrentWorkflows);
        }

        [Fact]
        public void NotAffectOriginal_WhenCloneIsModified()
        {
            // Arrange
            var original = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 100
            };
            var clone = (WorkflowForgeOptions)original.Clone();

            // Act
            clone.MaxConcurrentWorkflows = 200;

            // Assert
            Assert.Equal(100, original.MaxConcurrentWorkflows);
            Assert.Equal(200, clone.MaxConcurrentWorkflows);
        }

        [Fact]
        public void CloneTyped_ShouldReturnStronglyTypedCopy()
        {
            // Arrange
            var original = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 10
            };

            // Act - CloneTyped() returns WorkflowForgeOptions directly
            var clone = original.CloneTyped();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(10, clone.MaxConcurrentWorkflows);
        }
    }
}