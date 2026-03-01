using System;
using WorkflowForge.Options.Middleware;
using Xunit;

namespace WorkflowForge.Tests.Options;

/// <summary>
/// Unit tests for TimingMiddlewareOptions - defaults, property setters, boundary values, clone.
/// </summary>
public class TimingMiddlewareOptionsShould
{
    #region Constructor and Defaults

    [Fact]
    public void InitializeWithExpectedDefaults_GivenDefaultConstructor()
    {
        // Act
        var options = new TimingMiddlewareOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(TimingMiddlewareOptions.DefaultSectionName, options.SectionName);
        Assert.False(options.IncludeDetailedTimings);
    }

    [Fact]
    public void UseCustomSectionName_GivenCustomSectionName()
    {
        // Arrange
        var customSection = "Custom:Timing:Config";

        // Act
        var options = new TimingMiddlewareOptions(customSection);

        // Assert
        Assert.Equal(customSection, options.SectionName);
    }

    [Fact]
    public void BeExpectedValue_GivenDefaultSectionName()
    {
        Assert.Equal("WorkflowForge:Middleware:Timing", TimingMiddlewareOptions.DefaultSectionName);
    }

    #endregion Constructor and Defaults

    #region Property Setters

    [Fact]
    public void AllowSettingToTrue_GivenIncludeDetailedTimings()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();

        // Act
        options.IncludeDetailedTimings = true;

        // Assert
        Assert.True(options.IncludeDetailedTimings);
    }

    [Fact]
    public void AllowSettingToFalse_GivenIncludeDetailedTimings()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = true };

        // Act
        options.IncludeDetailedTimings = false;

        // Assert
        Assert.False(options.IncludeDetailedTimings);
    }

    [Fact]
    public void AllowSetting_GivenEnabled()
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
    public void ReturnEmptyList_GivenValidate()
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
    public void ReturnNewInstance_GivenClone()
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
    public void NotAffectOriginal_GivenModifyingClone()
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
    public void PreserveSectionName_GivenCloneWithCustomSectionName()
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
