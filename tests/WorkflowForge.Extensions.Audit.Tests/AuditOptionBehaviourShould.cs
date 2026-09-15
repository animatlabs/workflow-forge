using System.Linq;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Audit.Options;
using WorkflowForge.Operations;
using Xunit;

namespace WorkflowForge.Extensions.Audit.Tests;

/// <summary>
/// Asserts that each audit option changes what ends up in the written entries.
/// </summary>
public class AuditOptionBehaviourShould
{
    private static async Task<InMemoryAuditProvider> RunAsync(string name, AuditMiddlewareOptions options)
    {
        var provider = new InMemoryAuditProvider();
        var step = WorkflowOperations.Create("step", _ => (object?)"output");
        var workflow = WorkflowForge.CreateWorkflow(name)
            .AddOperation("seed", (foundry, _) =>
            {
                foundry.Properties["BusinessKey"] = "abc";
                return Task.CompletedTask;
            })
            .AddOperation(step)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry(name);
        foundry.AddMiddleware(new AuditMiddleware(provider, options, initiatedBy: "tester"));

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        return provider;
    }

    [Fact]
    public async Task OmitMetadataDurationAndInitiator_GivenMinimalDetail()
    {
        var provider = await RunAsync("AuditMinimal", new AuditMiddlewareOptions
        {
            DetailLevel = AuditDetailLevel.Minimal
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.Empty(completed.Metadata);
        Assert.Null(completed.DurationMs);
        Assert.Null(completed.InitiatedBy);
    }

    [Fact]
    public async Task IncludeDurationAndInitiator_GivenStandardDetail()
    {
        var provider = await RunAsync("AuditStandard", new AuditMiddlewareOptions
        {
            DetailLevel = AuditDetailLevel.Standard
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.NotNull(completed.DurationMs);
        Assert.Equal("tester", completed.InitiatedBy);
        Assert.True(completed.Metadata.ContainsKey("AuditTimestamp"));
        Assert.False(completed.Metadata.ContainsKey("BusinessKey"));
        Assert.False(completed.Metadata.ContainsKey("OutputData"));
    }

    [Fact]
    public async Task IncludeFoundryProperties_GivenVerboseDetail()
    {
        var provider = await RunAsync("AuditVerbose", new AuditMiddlewareOptions
        {
            DetailLevel = AuditDetailLevel.Verbose
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.Equal("abc", completed.Metadata["BusinessKey"]);
        Assert.False(completed.Metadata.ContainsKey("OutputData"));
    }

    [Fact]
    public async Task IncludePayloads_GivenCompleteDetail()
    {
        var provider = await RunAsync("AuditComplete", new AuditMiddlewareOptions
        {
            DetailLevel = AuditDetailLevel.Complete
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.Equal("output", completed.Metadata["OutputData"]);
    }

    [Fact]
    public async Task IncludePayloads_GivenLogDataPayloadsAtStandardDetail()
    {
        var provider = await RunAsync("AuditPayloads", new AuditMiddlewareOptions
        {
            DetailLevel = AuditDetailLevel.Standard,
            LogDataPayloads = true
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.Equal("output", completed.Metadata["OutputData"]);
    }

    [Fact]
    public async Task OmitInitiator_GivenUserContextDisabled()
    {
        var provider = await RunAsync("AuditNoUser", new AuditMiddlewareOptions
        {
            IncludeUserContext = false
        });

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);
        Assert.False(completed.Metadata.ContainsKey("InitiatedBy"));
    }

    [Fact]
    public async Task DropTheOldestEntry_WhenTheCapIsReached()
    {
        var provider = new InMemoryAuditProvider(2);

        for (var i = 0; i < 5; i++)
        {
            await provider.WriteAuditEntryAsync(new AuditEntry(
                System.Guid.NewGuid(),
                "wf",
                "op" + i,
                AuditEventType.OperationCompleted,
                "Completed"));
        }

        Assert.Equal(2, provider.Entries.Count);
        Assert.Equal(new[] { "op3", "op4" }, provider.Entries.Select(e => e.OperationName).ToArray());
    }

    [Fact]
    public void RejectANonPositiveCap()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new InMemoryAuditProvider(0));
    }
}
