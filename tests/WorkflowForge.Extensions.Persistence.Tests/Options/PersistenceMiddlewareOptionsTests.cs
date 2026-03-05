using WorkflowForge.Extensions.Persistence.Options;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Options;

public class PersistenceMiddlewareOptionsShould
{
    [Fact]
    public void HaveCorrectDefaults()
    {
        var options = new PersistenceMiddlewareOptions();

        Assert.True(options.PersistOnOperationComplete);
        Assert.True(options.PersistOnWorkflowComplete);
        Assert.True(options.PersistOnFailure);
        Assert.False(options.CompressData);
        Assert.Equal(10, options.MaxVersions);
    }

    [Fact]
    public void HaveCorrectDefaultSectionName()
    {
        Assert.Equal("WorkflowForge:Extensions:Persistence", PersistenceMiddlewareOptions.DefaultSectionName);
    }

    [Fact]
    public void UseDefaultSectionName_GivenDefaultConstructor()
    {
        var options = new PersistenceMiddlewareOptions();

        Assert.Equal(PersistenceMiddlewareOptions.DefaultSectionName, options.SectionName);
    }

    [Fact]
    public void UseCustomSectionName_GivenCustomConstructor()
    {
        var options = new PersistenceMiddlewareOptions("Custom:Section");

        Assert.Equal("Custom:Section", options.SectionName);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenValidSettings()
    {
        var options = new PersistenceMiddlewareOptions();

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenZeroMaxVersions()
    {
        var options = new PersistenceMiddlewareOptions { MaxVersions = 0 };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void ReturnValidationError_GivenNegativeMaxVersions()
    {
        var options = new PersistenceMiddlewareOptions { MaxVersions = -1 };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("MaxVersions", errors[0]);
    }

    [Fact]
    public void ProduceIndependentCopy_GivenClone()
    {
        var original = new PersistenceMiddlewareOptions
        {
            PersistOnOperationComplete = false,
            PersistOnWorkflowComplete = false,
            PersistOnFailure = false,
            CompressData = true,
            MaxVersions = 5
        };

        var clone = (PersistenceMiddlewareOptions)original.Clone();

        Assert.False(clone.PersistOnOperationComplete);
        Assert.False(clone.PersistOnWorkflowComplete);
        Assert.False(clone.PersistOnFailure);
        Assert.True(clone.CompressData);
        Assert.Equal(5, clone.MaxVersions);

        // Mutate clone, verify original unchanged
        clone.MaxVersions = 99;
        Assert.Equal(5, original.MaxVersions);
    }
}
