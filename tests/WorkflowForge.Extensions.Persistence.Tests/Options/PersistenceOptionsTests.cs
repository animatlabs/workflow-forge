using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Options
{
    public class PersistenceOptionsTests
    {
        [Fact]
        public void Constructor_WithDefaultSectionName_ShouldSetDefaultSectionName()
        {
            var options = new PersistenceOptions();
            Assert.Equal(PersistenceOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void Constructor_WithCustomSectionName_ShouldSetCustomSectionName()
        {
            var customSection = "MyApp:Persistence";
            var options = new PersistenceOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var options = new PersistenceOptions();
            Assert.True(options.Enabled);
            Assert.True(options.PersistOnOperationComplete);
            Assert.True(options.PersistOnWorkflowComplete);
            Assert.True(options.PersistOnFailure);
            Assert.Equal(0, options.MaxVersions);
        }

        [Fact]
        public void Validate_WithValidConfiguration_ShouldReturnEmptyErrors()
        {
            var options = new PersistenceOptions { MaxVersions = 10 };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithNegativeMaxVersions_ShouldReturnError()
        {
            var options = new PersistenceOptions { MaxVersions = -1 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("MaxVersions must be >= 0", errors[0]);
        }

        [Fact]
        public void Validate_WithZeroMaxVersions_ShouldReturnEmptyErrors()
        {
            var options = new PersistenceOptions { MaxVersions = 0 };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ErrorShouldIncludeSectionName()
        {
            var customSection = "Custom:Section";
            var options = new PersistenceOptions(customSection) { MaxVersions = -5 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains(customSection, errors[0]);
        }
    }
}