using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Configuration settings for circuit breaker behavior in WorkflowForge operations.
    /// </summary>
    public sealed class CircuitBreakerSettings : IValidatableObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether circuit breaker is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the failure threshold to open the circuit.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "FailureThreshold must be between 1 and 1000")]
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time window for failure counting.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:01", "01:00:00", 
               ErrorMessage = "TimeWindow must be between 1 second and 1 hour")]
        public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the duration to keep the circuit open.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:01", "01:00:00", 
               ErrorMessage = "OpenDuration must be between 1 second and 1 hour")]
        public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the number of test requests in half-open state.
        /// </summary>
        [Range(1, 100, ErrorMessage = "HalfOpenTestRequests must be between 1 and 100")]
        public int HalfOpenTestRequests { get; set; } = 1;

        /// <summary>
        /// Validates the circuit breaker settings.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (OpenDuration < TimeWindow)
            {
                results.Add(new ValidationResult(
                    "OpenDuration should typically be greater than or equal to TimeWindow",
                    new[] { nameof(OpenDuration), nameof(TimeWindow) }));
            }

            return results;
        }

        /// <summary>
        /// Creates a deep copy of the current circuit breaker settings.
        /// </summary>
        /// <returns>A new CircuitBreakerSettings instance with copied values.</returns>
        public CircuitBreakerSettings Clone()
        {
            return new CircuitBreakerSettings
            {
                IsEnabled = IsEnabled,
                FailureThreshold = FailureThreshold,
                TimeWindow = TimeWindow,
                OpenDuration = OpenDuration,
                HalfOpenTestRequests = HalfOpenTestRequests
            };
        }
    }
} 
