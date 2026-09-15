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
        }

        [Fact]
        public void ReturnEmptyErrors_GivenValidConfiguration()
        {
            var options = new PersistenceOptions { PersistOnFailure = false };
            var errors = options.Validate();
            Assert.Empty(errors);
        }
    }
}
