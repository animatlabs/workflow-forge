using System;
using WorkflowForge.Options.Middleware;
using Xunit;

namespace WorkflowForge.Tests.Options;

/// <summary>
/// Unit tests for LoggingMiddlewareOptions - defaults, property setters, validation, clone.
/// </summary>
public class LoggingMiddlewareOptionsShould
{
    #region Constructor and Defaults

    [Fact]
    public void InitializeWithExpectedDefaults_GivenDefaultConstructor()
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
    public void UseCustomSectionName_GivenCustomSectionName()
    {
        // Arrange
        var customSection = "Custom:Logging:Config";

        // Act
        var options = new LoggingMiddlewareOptions(customSection);

        // Assert
        Assert.Equal(customSection, options.SectionName);
    }

    [Fact]
    public void BeExpectedValue_GivenDefaultSectionName()
    {
        Assert.Equal("WorkflowForge:Middleware:Logging", LoggingMiddlewareOptions.DefaultSectionName);
    }

    #endregion Constructor and Defaults

    #region Property Setters

    [Fact]
    public void AllowSetting_GivenMinimumLevel()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions();

        // Act
        options.MinimumLevel = "Debug";

        // Assert
        Assert.Equal("Debug", options.MinimumLevel);
    }

    [Fact]
    public void AllowSettingToTrue_GivenLogDataPayloads()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions();

        // Act
        options.LogDataPayloads = true;

        // Assert
        Assert.True(options.LogDataPayloads);
    }

    [Fact]
    public void AllowSettingToFalse_GivenLogDataPayloads()
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { LogDataPayloads = true };

        // Act
        options.LogDataPayloads = false;

        // Assert
        Assert.False(options.LogDataPayloads);
    }

    [Fact]
    public void AllowSetting_GivenEnabled()
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
    public void ReturnEmptyList_GivenValidMinimumLevel()
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
    public void ReturnEmptyList_GivenValidLogLevels(string level)
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
    public void ReturnEmptyList_GivenValidLogLevelsCaseInsensitive(string level)
    {
        // Arrange
        var options = new LoggingMiddlewareOptions { MinimumLevel = level };

        // Act
        var errors = options.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ReturnError_GivenInvalidMinimumLevel()
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
    public void ReturnError_GivenEmptyMinimumLevel()
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
    public void ReturnNewInstance_GivenClone()
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
    public void NotAffectOriginal_GivenModifyingClone()
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
    public void PreserveSectionName_GivenCloneWithCustomSectionName()
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
