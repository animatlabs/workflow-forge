using System.ComponentModel.DataAnnotations;
using System.Linq;
using WorkflowForge.Extensions.Observability.Performance.Configurations;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class ObservabilitySettingsShould
{
    [Fact]
    public void HaveCorrectDefaults()
    {
        var settings = new ObservabilitySettings();

        Assert.True(settings.EnablePerformance);
        Assert.True(settings.EnableTracing);
        Assert.True(settings.EnableHealthChecks);
    }

    [Fact]
    public void HaveCorrectSectionName()
    {
        Assert.Equal("WorkflowForge:Observability", ObservabilitySettings.SectionName);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenDefaultSettings()
    {
        var settings = new ObservabilitySettings();
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenAllDisabledSettings()
    {
        var settings = new ObservabilitySettings
        {
            EnablePerformance = false,
            EnableTracing = false,
            EnableHealthChecks = false
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ProduceIndependentCopy_GivenClone()
    {
        var original = new ObservabilitySettings
        {
            EnablePerformance = false,
            EnableTracing = false,
            EnableHealthChecks = false
        };

        var clone = original.Clone();

        Assert.False(clone.EnablePerformance);
        Assert.False(clone.EnableTracing);
        Assert.False(clone.EnableHealthChecks);

        // Mutate clone, verify original unchanged
        clone.EnablePerformance = true;
        Assert.False(original.EnablePerformance);
    }
}
