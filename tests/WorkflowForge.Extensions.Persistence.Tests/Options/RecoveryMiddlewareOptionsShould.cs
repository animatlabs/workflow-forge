using System;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Options
{
    public class RecoveryMiddlewareOptionsShould
    {
        [Fact]
        public void SetDefaultSectionName_GivenDefaultSectionName()
        {
            var options = new RecoveryMiddlewareOptions();
            Assert.Equal(RecoveryMiddlewareOptions.DefaultSectionName, options.SectionName);
        }

        [Fact]
        public void SetCustomSectionName_GivenCustomSectionName()
        {
            var customSection = "MyApp:Recovery";
            var options = new RecoveryMiddlewareOptions(customSection);
            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void BeCorrect_GivenDefaultValues()
        {
            var options = new RecoveryMiddlewareOptions();
            Assert.True(options.Enabled);
            Assert.Equal(3, options.MaxRetryAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), options.BaseDelay);
            Assert.True(options.UseExponentialBackoff);
            Assert.True(options.AttemptResume);
            Assert.True(options.LogRecoveryAttempts);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenValidConfiguration()
        {
            var options = new RecoveryMiddlewareOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromSeconds(2) };
            var errors = options.Validate();
            Assert.Empty(errors);
        }

        [Fact]
        public void ReturnError_GivenMaxRetryAttemptsBelowMinimum()
        {
            var options = new RecoveryMiddlewareOptions { MaxRetryAttempts = 0 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("MaxRetryAttempts must be between 1 and 100", errors[0]);
        }

        [Fact]
        public void ReturnError_GivenMaxRetryAttemptsAboveMaximum()
        {
            var options = new RecoveryMiddlewareOptions { MaxRetryAttempts = 101 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("MaxRetryAttempts must be between 1 and 100", errors[0]);
        }

        [Fact]
        public void ReturnError_GivenBaseDelayTooLarge()
        {
            var options = new RecoveryMiddlewareOptions { BaseDelay = TimeSpan.FromMinutes(11) };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("BaseDelay must be between 0 and 10 minutes", errors[0]);
        }

        [Fact]
        public void ReturnError_GivenNegativeBaseDelay()
        {
            var options = new RecoveryMiddlewareOptions { BaseDelay = TimeSpan.FromSeconds(-1) };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains("BaseDelay must be between 0 and 10 minutes", errors[0]);
        }

        [Fact]
        public void IncludeSectionNameInError_GivenInvalidConfiguration()
        {
            var customSection = "Custom:Section";
            var options = new RecoveryMiddlewareOptions(customSection) { MaxRetryAttempts = 0 };
            var errors = options.Validate();
            Assert.Single(errors);
            Assert.Contains(customSection, errors[0]);
        }
    }
}
