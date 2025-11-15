using WorkflowForge.Options;

namespace WorkflowForge.Tests.Configuration.WorkflowForgeOptionsTests
{
    /// <summary>
    /// Tests for WorkflowForgeOptions validation logic
    /// </summary>
    public class ValidateShould
    {
        [Fact]
        public void ReturnNoErrors_GivenDefaultOptions()
        {
            // Arrange
            var options = new WorkflowForgeOptions();

            // Act
            var errors = options.Validate();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnNoErrors_GivenValidConfiguration()
        {
            // Arrange
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 50
            };

            // Act
            var errors = options.Validate();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnError_GivenNegativeMaxConcurrentWorkflows()
        {
            // Arrange
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = -1
            };

            // Act
            var errors = options.Validate();

            // Assert
            Assert.Single(errors);
            Assert.Contains("MaxConcurrentWorkflows must be between 0 and 10000", errors[0]);
        }

        [Fact]
        public void ReturnError_GivenMaxConcurrentWorkflowsExceedsLimit()
        {
            // Arrange
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 10001
            };

            // Act
            var errors = options.Validate();

            // Assert
            Assert.Single(errors);
            Assert.Contains("MaxConcurrentWorkflows must be between 0 and 10000", errors[0]);
        }

        [Fact]
        public void AcceptZeroForUnlimitedWorkflows()
        {
            // Arrange
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 0
            };

            // Act
            var errors = options.Validate();

            // Assert
            Assert.Empty(errors);
        }
    }
}