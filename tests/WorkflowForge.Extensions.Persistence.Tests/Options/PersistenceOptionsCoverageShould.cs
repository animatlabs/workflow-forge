using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Options;

public class PersistenceOptionsCoverageShould
{
    [Fact]
    public void CopyEveryValue_WhenCloned()
    {
        var original = new PersistenceOptions("Custom:Persistence")
        {
            Enabled = false,
            PersistOnOperationComplete = false,
            PersistOnWorkflowComplete = false,
            PersistOnFailure = false,
            InstanceId = "instance-1",
            WorkflowKey = "workflow-1"
        };

        var clone = (PersistenceOptions)original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal("Custom:Persistence", clone.SectionName);
        Assert.False(clone.Enabled);
        Assert.False(clone.PersistOnOperationComplete);
        Assert.False(clone.PersistOnWorkflowComplete);
        Assert.False(clone.PersistOnFailure);
        Assert.Equal("instance-1", clone.InstanceId);
        Assert.Equal("workflow-1", clone.WorkflowKey);
    }

    [Fact]
    public void ReturnNoErrors_WhenValidated()
    {
        Assert.Empty(new PersistenceOptions().Validate());
        Assert.Empty(new PersistenceOptions("Custom") { Enabled = false }.Validate());
    }

    [Fact]
    public void UseTheDefaultSection_WhenNoneSupplied()
    {
        Assert.Equal("WorkflowForge:Extensions:Persistence", PersistenceOptions.DefaultSectionName);
        Assert.Equal(PersistenceOptions.DefaultSectionName, new PersistenceOptions().SectionName);
    }
}
