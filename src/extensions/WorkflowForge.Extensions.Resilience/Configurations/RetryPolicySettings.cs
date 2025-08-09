using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience.Configurations
{
    /// <summary>
    /// Configuration settings for retry policies in WorkflowForge operations.
    /// </summary>
    public sealed class RetryPolicySettings : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the retry strategy type.
        /// </summary>
        public RetryStrategyType StrategyType { get; set; } = RetryStrategyType.None;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        [Range(0, 100, ErrorMessage = "MaxAttempts must be between 0 and 100")]
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retries.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:00", "00:10:00",
               ErrorMessage = "BaseDelay must be between 0 and 10 minutes")]
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:00", "01:00:00",
               ErrorMessage = "MaxDelay must be between 0 and 1 hour")]
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the backoff multiplier for exponential strategies.
        /// </summary>
        [Range(1.1, 10.0, ErrorMessage = "BackoffMultiplier must be between 1.1 and 10.0")]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets whether to add jitter to retry delays.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Gets a predefined no-retry policy configuration.
        /// </summary>
        public static RetryPolicySettings NoRetry => new()
        {
            StrategyType = RetryStrategyType.None,
            MaxAttempts = 0
        };

        /// <summary>
        /// Gets a predefined exponential backoff configuration.
        /// </summary>
        public static RetryPolicySettings DefaultExponentialBackoff => new()
        {
            StrategyType = RetryStrategyType.ExponentialBackoff,
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            UseJitter = true
        };

        /// <summary>
        /// Validates the retry policy settings.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (MaxDelay < BaseDelay)
            {
                results.Add(new ValidationResult(
                    "MaxDelay cannot be less than BaseDelay",
                    new[] { nameof(MaxDelay), nameof(BaseDelay) }));
            }

            if (StrategyType == RetryStrategyType.ExponentialBackoff && BackoffMultiplier <= 1.0)
            {
                results.Add(new ValidationResult(
                    "BackoffMultiplier must be greater than 1.0 for exponential backoff strategy",
                    new[] { nameof(BackoffMultiplier) }));
            }

            if (StrategyType != RetryStrategyType.None && MaxAttempts <= 0)
            {
                results.Add(new ValidationResult(
                    "MaxAttempts must be greater than 0 when using retry strategies",
                    new[] { nameof(MaxAttempts) }));
            }

            return results;
        }

        /// <summary>
        /// Creates a deep copy of the current retry policy settings.
        /// </summary>
        /// <returns>A new RetryPolicySettings instance with copied values.</returns>
        public RetryPolicySettings Clone()
        {
            return new RetryPolicySettings
            {
                StrategyType = StrategyType,
                MaxAttempts = MaxAttempts,
                BaseDelay = BaseDelay,
                MaxDelay = MaxDelay,
                BackoffMultiplier = BackoffMultiplier,
                UseJitter = UseJitter
            };
        }
    }
}