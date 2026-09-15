using WorkflowForge.Extensions.Audit.Options;
using Xunit;

namespace WorkflowForge.Extensions.Audit.Tests;

public class AuditMiddlewareOptionsCoverageShould
{
    [Fact]
    public void CopyEveryValue_WhenCloned()
    {
        var original = new AuditMiddlewareOptions("Custom:Audit")
        {
            Enabled = false,
            DetailLevel = AuditDetailLevel.Complete,
            LogDataPayloads = true,
            IncludeTimestamps = false,
            IncludeUserContext = false
        };

        var clone = (AuditMiddlewareOptions)original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal("Custom:Audit", clone.SectionName);
        Assert.False(clone.Enabled);
        Assert.Equal(AuditDetailLevel.Complete, clone.DetailLevel);
        Assert.True(clone.LogDataPayloads);
        Assert.False(clone.IncludeTimestamps);
        Assert.False(clone.IncludeUserContext);
    }

    [Fact]
    public void ReturnNoErrors_WhenValidated()
    {
        Assert.Empty(new AuditMiddlewareOptions().Validate());
        Assert.Empty(new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Minimal }.Validate());
    }

    [Fact]
    public void DefaultToStandardDetailAndNoPayloads()
    {
        var options = new AuditMiddlewareOptions();

        Assert.True(options.Enabled);
        Assert.Equal(AuditDetailLevel.Standard, options.DetailLevel);
        Assert.False(options.LogDataPayloads);
        Assert.True(options.IncludeTimestamps);
        Assert.True(options.IncludeUserContext);
        Assert.Equal(AuditMiddlewareOptions.DefaultSectionName, options.SectionName);
    }
}
