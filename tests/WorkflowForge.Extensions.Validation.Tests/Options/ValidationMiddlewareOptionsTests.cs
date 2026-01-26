using WorkflowForge.Extensions.Validation.Options;

namespace WorkflowForge.Extensions.Validation.Tests.Options
{
    public class ValidationMiddlewareOptionsTests
    {
        [Fact]
        public void Constructor_WithDefaultSectionName_ShouldSetDefaultSectionName()
        {
            var options = new ValidationMiddlewareOptions();
            Assert.Equal(ValidationMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void Constructor_WithCustomSectionName_ShouldSetCustomSectionName()
        {
            var customSection = "MyApp:Validation";
            var options = new ValidationMiddlewareOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void Constructor_WithNullSectionName_ShouldUseDefaultSectionName()
        {
            var options = new ValidationMiddlewareOptions(null!);
            Assert.Equal(ValidationMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var options = new ValidationMiddlewareOptions();
            Assert.True(options.Enabled);
            Assert.False(options.IgnoreValidationFailures);
            Assert.True(options.ThrowOnValidationError);
            Assert.True(options.LogValidationErrors);
            Assert.True(options.StoreValidationResults);
        }

        [Fact]
        public void Validate_WithValidConfiguration_ShouldReturnEmptyErrors()
        {
            var options = new ValidationMiddlewareOptions { Enabled = true, IgnoreValidationFailures = false, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithIgnoreFailuresAndThrowOnError_ShouldReturnError()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = true, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("IgnoreValidationFailures=true and ThrowOnValidationError=true", errors[0]);
        }

        [Fact]
        public void Validate_WithIgnoreFailuresAndNoThrow_ShouldReturnEmptyErrors()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = true, ThrowOnValidationError = false };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithThrowOnErrorAndNoIgnore_ShouldReturnEmptyErrors()
        {
            var options = new ValidationMiddlewareOptions { IgnoreValidationFailures = false, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ErrorShouldIncludeSectionName()
        {
            var customSection = "Custom:Section";
            var options = new ValidationMiddlewareOptions(customSection) { IgnoreValidationFailures = true, ThrowOnValidationError = true };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains(customSection, errors[0]);
        }
    }
}