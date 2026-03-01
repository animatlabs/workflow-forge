using System;
using WorkflowForge.Options.Middleware;
using Xunit;

namespace WorkflowForge.Tests.Options;

/// <summary>
/// Unit tests for LoggingMiddlewareOptions - defaults, property setters, validation, clone.
/// </summary>
public class LoggingMiddlewareOptionsTests
{
    #region Constructor and Defaults

    [Fact]
    public void Constructor_Default_InitializesWithExpectedDefaults()
    {
        // Act
        var options = new LoggingMiddlewareOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(LoggingMiddlewareOptions.DefaultSectionName, options.SectionName);
        Assert.Equal("Information", options.MinimumLevel);
        Assert.False(options.LogDataPayloads);
    }

    [Fact]
    public void Constructor_WithCustomSectionName_UseCustomSectionName()
    {
        // Arrange
        var customSection = "Custom:Logging:Config";

        // Act
        var options = new LoggingMiddlewareOptions(customSection);

        // Assert
        Assert.Equal(customSection, options.SectionName);
    }

    [Fact]
    public void DefaultSectionName_IsExpectedValue()
    {
        Assert.Equal("WorkflowForge:Middleware:Logging", LoggingMiddlewareOptions.DefaultSectionName);
    }

    #endregion Constructor and Defaults

    #region Property Setters

    [Fact]
    public void MinimumLevel_CanBeSet()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions();

        // Act
        options.MinimumLevel = "Debug";

        // Assert
        Assert.Equal("Debug", options.MinimumLevel);
    }

    [Fact]
    public void LogDataPayloads_CanBeSetToTrue()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions();

        // Act
        options.LogDataPayloads = true;

        // Assert
        Assert.True(options.LogDataPayloads);
    }

    [Fact]
    public void LogDataPayloads_CanBeSetToFalse()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { LogDataPayloads = true };

        // Act
        options.LogDataPayloads = false;

        // Assert
        Assert.False(options.LogDataPayloads);
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions();

        // Act
        options.Enabled = false;

        // Assert
        Assert.False(options.Enabled);
    }

    #endregion Property Setters

    #region Validate

    [Fact]
    public void Validate_WithValidMinimumLevel_ReturnsEmptyList()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = "Information" };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Trace")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void Validate_WithValidLogLevels_ReturnsEmptyList(string level)
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = level };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("trace")]
    [InlineData("DEBUG")]
    [InlineData("information")]
    public void Validate_WithValidLogLevelsCaseInsensitive_ReturnsEmptyList(string level)
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = level };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithInvalidMinimumLevel_ReturnsError()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = "InvalidLevel" };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("MinimumLevel", errors[0]);
        Assert.Contains("InvalidLevel", errors[0]);
    }

    [Fact]
    public void Validate_WithEmptyMinimumLevel_ReturnsError()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = "" };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.Single(errors);
    }

    #endregion Validate

    #region Clone

    [Fact]
    public void Clone_ReturnsNewInstance()
    {
        // Arrange
        var original = new LoggingMiddlewareOptions
        {
            Enabled = false,
            MinimumLevel = "Debug",
            LogDataPayloads = true
        };

        // Act
        var clone = (LoggingMiddlewareOptions)original.Clone();

        // Assert
        Assert.NotNull(clone);
        Assert.NotSame(original, clone);
        Assert.Equal(original.Enabled, clone.Enabled);
        Assert.Equal(original.MinimumLevel, clone.MinimumLevel);
        Assert.Equal(original.LogDataPayloads, clone.LogDataPayloads);
        Assert.Equal(original.SectionName, clone.SectionName);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        var original = new LoggingMiddlewareOptions { LogDataPayloads = true };
        var clone = (LoggingMiddlewareOptions)original.Clone();

        // Act
        clone.LogDataPayloads = false;

        // Assert
        Assert.True(original.LogDataPayloads);
        Assert.False(clone.LogDataPayloads);
    }

    [Fact]
    public void Clone_WithCustomSectionName_PreservesSectionName()
    {
        // Arrange
        var original = new LoggingMiddlewareOptions("Custom:Section");

        // Act
        var clone = (LoggingMiddlewareOptions)original.Clone();

        // Assert
        Assert.Equal("Custom:Section", clone.SectionName);
    }

    #endregion Clone
}
