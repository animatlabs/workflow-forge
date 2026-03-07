using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WorkflowForge.Extensions.Resilience.Configurations;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class CircuitBreakerSettingsShould
{
    [Fact]
    public void HaveCorrectDefaults_GivenDefaultConstructor()
    {
        var settings = new CircuitBreakerSettings();

        Assert.False(settings.IsEnabled);
        Assert.Equal(5, settings.FailureThreshold);
        Assert.Equal(TimeSpan.FromMinutes(1), settings.TimeWindow);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.OpenDuration);
        Assert.Equal(1, settings.HalfOpenTestRequests);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenValidSettings()
    {
        var settings = new CircuitBreakerSettings
        {
            OpenDuration = TimeSpan.FromMinutes(2),
            TimeWindow = TimeSpan.FromMinutes(1)
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ReturnValidationError_GivenOpenDurationLessThanTimeWindow()
    {
        var settings = new CircuitBreakerSettings
        {
            OpenDuration = TimeSpan.FromSeconds(10),
            TimeWindow = TimeSpan.FromMinutes(1)
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Single(results);
        Assert.Contains("OpenDuration", results[0].MemberNames);
        Assert.Contains("TimeWindow", results[0].MemberNames);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenEqualDurations()
    {
        var settings = new CircuitBreakerSettings
        {
            OpenDuration = TimeSpan.FromMinutes(1),
            TimeWindow = TimeSpan.FromMinutes(1)
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ProduceIndependentCopy_GivenClone()
    {
        var original = new CircuitBreakerSettings
        {
            IsEnabled = true,
            FailureThreshold = 10,
            TimeWindow = TimeSpan.FromSeconds(30),
            OpenDuration = TimeSpan.FromMinutes(2),
            HalfOpenTestRequests = 3
        };

        var clone = original.Clone();

        Assert.True(clone.IsEnabled);
        Assert.Equal(10, clone.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), clone.TimeWindow);
        Assert.Equal(TimeSpan.FromMinutes(2), clone.OpenDuration);
        Assert.Equal(3, clone.HalfOpenTestRequests);

        // Mutate clone, verify original unchanged
        clone.FailureThreshold = 99;
        Assert.Equal(10, original.FailureThreshold);
    }
}
