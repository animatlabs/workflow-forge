using System;
using WorkflowForge.Options.Middleware;
using Xunit;

namespace WorkflowForge.Tests.Options;

/// <summary>
/// Unit tests for TimingMiddlewareOptions - defaults, property setters, boundary values, clone.
/// </summary>
public class TimingMiddlewareOptionsTests
{
    #region Constructor and Defaults

    [Fact]
    public void Constructor_Default_InitializesWithExpectedDefaults()
    {
        // Act
        var options = new TimingMiddlewareOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(TimingMiddlewareOptions.DefaultSectionName, options.SectionName);
        Assert.False(options.IncludeDetailedTimings);
    }

    [Fact]
    public void Constructor_WithCustomSectionName_UseCustomSectionName()
    {
        // Arrange
        var customSection = "Custom:Timing:Config";

        // Act
        var options = new TimingMiddlewareOptions(customSection);

        // Assert
        Assert.Equal(customSection, options.SectionName);
    }

    [Fact]
    public void DefaultSectionName_IsExpectedValue()
    {
        Assert.Equal("WorkflowForge:Middleware:Timing", TimingMiddlewareOptions.DefaultSectionName);
    }

    #endregion Constructor and Defaults

    #region Property Setters

    [Fact]
    public void IncludeDetailedTimings_CanBeSetToTrue()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();

        // Act
        options.IncludeDetailedTimings = true;

        // Assert
        Assert.True(options.IncludeDetailedTimings);
    }

    [Fact]
    public void IncludeDetailedTimings_CanBeSetToFalse()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = true };

        // Act
        options.IncludeDetailedTimings = false;

        // Assert
        Assert.False(options.IncludeDetailedTimings);
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();

        // Act
        options.Enabled = false;

        // Assert
        Assert.False(options.Enabled);
    }

    #endregion Property Setters

    #region Validate

    [Fact]
    public void Validate_AlwaysReturnsEmptyList()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();

        // Act
        var errors = options.Validate();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    #endregion Validate

    #region Clone

    [Fact]
    public void Clone_ReturnsNewInstance()
    {
        // Arrange
        var original = new TimingMiddlewareOptions
        {
            Enabled = false,
            IncludeDetailedTimings = true
        };

        // Act
        var clone = (TimingMiddlewareOptions)original.Clone();

        // Assert
        Assert.NotNull(clone);
        Assert.NotSame(original, clone);
        Assert.Equal(original.Enabled, clone.Enabled);
        Assert.Equal(original.IncludeDetailedTimings, clone.IncludeDetailedTimings);
        Assert.Equal(original.SectionName, clone.SectionName);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        var original = new TimingMiddlewareOptions { IncludeDetailedTimings = true };
        var clone = (TimingMiddlewareOptions)original.Clone();

        // Act
        clone.IncludeDetailedTimings = false;

        // Assert
        Assert.True(original.IncludeDetailedTimings);
        Assert.False(clone.IncludeDetailedTimings);
    }

    [Fact]
    public void Clone_WithCustomSectionName_PreservesSectionName()
    {
        // Arrange
        var original = new TimingMiddlewareOptions("Custom:Section");

        // Act
        var clone = (TimingMiddlewareOptions)original.Clone();

        // Assert
        Assert.Equal("Custom:Section", clone.SectionName);
    }

    #endregion Clone
}
