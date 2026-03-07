using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WorkflowForge.Extensions.Observability.Performance.Configurations;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class PerformanceSettingsShould
{
    [Fact]
    public void HaveCorrectDefaults_GivenDefaultConstructor()
    {
        var settings = new PerformanceSettings();

        Assert.Equal(Environment.ProcessorCount, settings.MaxDegreeOfParallelism);
        Assert.True(settings.EnableObjectPooling);
        Assert.Equal(1000, settings.MaxQueuedOperations);
        Assert.Equal(10, settings.BatchSize);
        Assert.True(settings.EnableMemoryOptimization);
        Assert.Equal("Balanced", settings.GarbageCollectionMode);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenDefaultSettings()
    {
        var settings = new PerformanceSettings();
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Balanced")]
    [InlineData("LowLatency")]
    [InlineData("HighThroughput")]
    public void ReturnNoValidationErrors_GivenValidGcMode(string gcMode)
    {
        var settings = new PerformanceSettings { GarbageCollectionMode = gcMode };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData("aggressive")]
    public void ReturnValidationError_GivenInvalidGcMode(string gcMode)
    {
        var settings = new PerformanceSettings { GarbageCollectionMode = gcMode };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Single(results);
        Assert.Contains("GarbageCollectionMode", results[0].ErrorMessage!);
    }

    [Fact]
    public void ProduceIndependentCopy_GivenClone()
    {
        var original = new PerformanceSettings
        {
            MaxDegreeOfParallelism = 4,
            EnableObjectPooling = false,
            MaxQueuedOperations = 500,
            BatchSize = 20,
            EnableMemoryOptimization = false,
            GarbageCollectionMode = "LowLatency"
        };

        var clone = original.Clone();

        Assert.Equal(4, clone.MaxDegreeOfParallelism);
        Assert.False(clone.EnableObjectPooling);
        Assert.Equal(500, clone.MaxQueuedOperations);
        Assert.Equal(20, clone.BatchSize);
        Assert.False(clone.EnableMemoryOptimization);
        Assert.Equal("LowLatency", clone.GarbageCollectionMode);

        // Mutate clone, verify original unchanged
        clone.MaxDegreeOfParallelism = 8;
        Assert.Equal(4, original.MaxDegreeOfParallelism);
    }
}
