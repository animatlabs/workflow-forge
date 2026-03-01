using WorkflowForge.Extensions.Validation.Options;

namespace WorkflowForge.Extensions.Validation.Tests.Options
{
    public class ValidationMiddlewareOptionsShould
    {
        [Fact]
        public void SetDefaultSectionName_GivenDefaultSectionName()
        {
            var options = new ValidationMiddlewareOptions();
            Assert.Equal(ValidationMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void SetCustomSectionName_GivenCustomSectionName()
        {
            var customSection = "MyApp:Validation";
            var options = new ValidationMiddlewareOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void UseDefaultSectionName_GivenNullSectionName()
        {
            var options = new ValidationMiddlewareOptions(null!);
            Assert.Equal(ValidationMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void BeCorrect_GivenDefaultValues()
        {
            var options = new ValidationMiddlewareOptions();
            Assert.True(options.Enabled);
            Assert.False(options.IgnoreValidationFailures);
            Assert.True(options.ThrowOnValidationError);
            Assert.True(options.LogValidationErrors);
            Assert.True(options.StoreValidationResults);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenValidConfiguration()
        {
            var options = new ValidationMiddlewareOptions { Enabled = true, IgnoreValidationFailures = false, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnError_GivenIgnoreFailuresAndThrowOnError()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = true, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("IgnoreValidationFailures=true and ThrowOnValidationError=true", errors[0]);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenIgnoreFailuresAndNoThrow()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = true, ThrowOnValidationError = false };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenThrowOnErrorAndNoIgnore()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = false, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void IncludeSectionNameInError_GivenInvalidConfiguration()
        {
            var customSection = "Custom:Section";
            var options = new ValidationMiddlewareOptions(customSection) { IgnoreValidationFailures = true, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains(customSection, errors[0]);
        }
    }
}