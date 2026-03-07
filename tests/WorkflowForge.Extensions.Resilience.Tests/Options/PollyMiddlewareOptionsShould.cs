using WorkflowForge.Extensions.Resilience.Polly.Options;

namespace WorkflowForge.Extensions.Resilience.Tests.Options
{
    public class PollyMiddlewareOptionsShould
    {
        [Fact]
        public void SetDefaultSectionName_GivenDefaultSectionName()
        {
            var options = new PollyMiddlewareOptions();
            Assert.Equal(PollyMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void SetCustomSectionName_GivenCustomSectionName()
        {
            var customSection = "MyApp:Polly";
            var options = new PollyMiddlewareOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void BeCorrect_GivenDefaultValues()
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
        public void ReturnEmptyErrors_GivenValidConfiguration()
        {
            var options = new PollyMiddlewareOptions { Retry = { IsEnabled = true, MaxRetryAttempts = 5 } };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnError_GivenInvalidRetryAttempts()
        {
            var options = new PollyMiddlewareOptions { Retry = { IsEnabled = true, MaxRetryAttempts = 101 } };
            var errors = options.Validate();
            Assert.NotEmpty(errors);
            Assert.Contains("MaxRetryAttempts must be between 0 and 100", errors[0]);
        }
    }
}
