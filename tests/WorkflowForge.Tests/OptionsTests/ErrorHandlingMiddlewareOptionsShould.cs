using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Tests.OptionsTests;

/// <summary>
/// Unit tests for ErrorHandlingMiddlewareOptions - defaults, property setters, boundary values, clone.
/// </summary>
public class ErrorHandlingMiddlewareOptionsShould
{
    #region Constructor and Defaults

    [Fact]
    public void InitializeWithExpectedDefaults_GivenDefaultConstructor()
    {
        // Act
        var options = new ErrorHandlingMiddlewareOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(ErrorHandlingMiddlewareOptions.DefaultSectionName, options.SectionName);
        Assert.True(options.RethrowExceptions);
        Assert.True(options.IncludeStackTraces);
    }

    [Fact]
    public void UseCustomSectionName_GivenCustomSectionName()
    {
        // Arrange
        var customSection = "Custom:ErrorHandling:Config";

        // Act
        var options = new ErrorHandlingMiddlewareOptions(customSection);

        // Assert
        Assert.Equal(customSection, options.SectionName);
    }

    [Fact]
    public void BeExpectedValue_GivenDefaultSectionName()
    {
        Assert.Equal("WorkflowForge:Middleware:ErrorHandling", ErrorHandlingMiddlewareOptions.DefaultSectionName);
    }

    #endregion Constructor and Defaults

    #region Property Setters

    [Fact]
    public void AllowSettingToFalse_GivenRethrowExceptions()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions();

        // Act
        options.RethrowExceptions = false;

        // Assert
        Assert.False(options.RethrowExceptions);
    }

    [Fact]
    public void AllowSettingToTrue_GivenRethrowExceptions()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = false };

        // Act
        options.RethrowExceptions = true;

        // Assert
        Assert.True(options.RethrowExceptions);
    }

    [Fact]
    public void AllowSettingToFalse_GivenIncludeStackTraces()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions();

        // Act
        options.IncludeStackTraces = false;

        // Assert
        Assert.False(options.IncludeStackTraces);
    }

    [Fact]
    public void AllowSettingToTrue_GivenIncludeStackTraces()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions { IncludeStackTraces = false };

        // Act
        options.IncludeStackTraces = true;

        // Assert
        Assert.True(options.IncludeStackTraces);
    }

    [Fact]
    public void AllowSetting_GivenEnabled()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions();

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
        var options = new ErrorHandlingMiddlewareOptions();

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
        var original = new ErrorHandlingMiddlewareOptions
        {
            Enabled = false,
            RethrowExceptions = false,
            IncludeStackTraces = false
        };

        // Act
        var clone = (ErrorHandlingMiddlewareOptions)original.Clone();

        // Assert
        Assert.NotNull(clone);
        Assert.NotSame(original, clone);
        Assert.Equal(original.Enabled, clone.Enabled);
        Assert.Equal(original.RethrowExceptions, clone.RethrowExceptions);
        Assert.Equal(original.IncludeStackTraces, clone.IncludeStackTraces);
        Assert.Equal(original.SectionName, clone.SectionName);
    }

    [Fact]
    public void NotAffectOriginal_GivenModifyingClone()
    {
        // Arrange
        var original = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var clone = (ErrorHandlingMiddlewareOptions)original.Clone();

        // Act
        clone.RethrowExceptions = false;

        // Assert
        Assert.True(original.RethrowExceptions);
        Assert.False(clone.RethrowExceptions);
    }

    [Fact]
    public void PreserveSectionName_GivenCloneWithCustomSectionName()
    {
        // Arrange
        var original = new ErrorHandlingMiddlewareOptions("Custom:Section");

        // Act
        var clone = (ErrorHandlingMiddlewareOptions)original.Clone();

        // Assert
        Assert.Equal("Custom:Section", clone.SectionName);
    }

    #endregion Clone
}
