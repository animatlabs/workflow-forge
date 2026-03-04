using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Options
{
    public class PersistenceOptionsShould
    {
        [Fact]
        public void SetDefaultSectionName_GivenDefaultSectionName()
        {
            var options = new PersistenceOptions();
            Assert.Equal(PersistenceOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void SetCustomSectionName_GivenCustomSectionName()
        {
            var customSection = "MyApp:Persistence";
            var options = new PersistenceOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void BeCorrect_GivenDefaultValues()
        {
            var options = new PersistenceOptions();
            Assert.True(options.Enabled);
            Assert.True(options.PersistOnOperationComplete);
            Assert.True(options.PersistOnWorkflowComplete);
            Assert.True(options.PersistOnFailure);
            Assert.Equal(0, options.MaxVersions);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenValidConfiguration()
        {
            var options = new PersistenceOptions { MaxVersions = 10 };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnError_GivenNegativeMaxVersions()
        {
            var options = new PersistenceOptions { MaxVersions = -1 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("MaxVersions must be >= 0", errors[0]);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenZeroMaxVersions()
        {
            var options = new PersistenceOptions { MaxVersions = 0 };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void IncludeSectionNameInError_GivenInvalidConfiguration()
        {
            var customSection = "Custom:Section";
            var options = new PersistenceOptions(customSection) { MaxVersions = -5 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains(customSection, errors[0]);
        }

        [Fact]
        public void ProduceIndependentCopy_GivenClone()
        {
            var original = new PersistenceOptions("My:Section")
            {
                Enabled = false,
                PersistOnOperationComplete = false,
                PersistOnWorkflowComplete = false,
                PersistOnFailure = false,
                MaxVersions = 5,
                InstanceId = "instance-123",
                WorkflowKey = "workflow-abc"
            };

            var clone = (PersistenceOptions)original.Clone();

            Assert.Equal("My:Section", clone.SectionName);
            Assert.False(clone.Enabled);
            Assert.False(clone.PersistOnOperationComplete);
            Assert.False(clone.PersistOnWorkflowComplete);
            Assert.False(clone.PersistOnFailure);
            Assert.Equal(5, clone.MaxVersions);
            Assert.Equal("instance-123", clone.InstanceId);
            Assert.Equal("workflow-abc", clone.WorkflowKey);

            // Verify independence
            clone.MaxVersions = 10;
            Assert.Equal(5, original.MaxVersions);
        }

        [Fact]
        public void ClonePreservesNullInstanceIdAndWorkflowKey_GivenDefaultOptions()
        {
            var original = new PersistenceOptions();

            var clone = (PersistenceOptions)original.Clone();

            Assert.Null(clone.InstanceId);
            Assert.Null(clone.WorkflowKey);
        }

        [Fact]
        public void SupportInstanceIdAndWorkflowKey_GivenPropertyAssignment()
        {
            var options = new PersistenceOptions
            {
                InstanceId = "my-instance",
                WorkflowKey = "my-workflow"
            };

            Assert.Equal("my-instance", options.InstanceId);
            Assert.Equal("my-workflow", options.WorkflowKey);
        }
    }
}