using WorkflowForge.Extensions.Resilience.Polly.Options;

namespace WorkflowForge.Extensions.Resilience.Tests.Options
{
    public class PollyMiddlewareOptionsTests
    {
        [Fact]
        public void Constructor_WithDefaultSectionName_ShouldSetDefaultSectionName()
        {
            var options = new PollyMiddlewareOptions();
            Assert.Equal(PollyMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void Constructor_WithCustomSectionName_ShouldSetCustomSectionName()
        {
            var customSection = "MyApp:Polly";
            var options = new PollyMiddlewareOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var options = new PollyMiddlewareOptions();
            Assert.True(options.Enabled);
            Assert.NotNull(options.Retry);
            Assert.NotNull(options.CircuitBreaker);
            Assert.NotNull(options.Timeout);
            Assert.NotNull(options.RateLimiter);
            Assert.False(options.EnableComprehensivePolicies);
            Assert.True(options.EnableDetailedLogging);
        }

        [Fact]
        public void Validate_WithValidConfiguration_ShouldReturnEmptyErrors()
        {
            var options = new PollyMiddlewareOptions { Retry = { IsEnabled = true, MaxRetryAttempts = 5 } };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithInvalidRetryAttempts_ShouldReturnError()
        {
            var options = new PollyMiddlewareOptions { Retry = { IsEnabled = true, MaxRetryAttempts = 101 } };
            var errors = options.Validate();
            Assert.NotEmpty(errors);
            Assert.Contains("MaxRetryAttempts must be between 0 and 100", errors[0]);
        }
    }
}
