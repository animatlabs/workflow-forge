using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Audit.Options;
using WorkflowForge.Operations;
using AuditTrailMiddleware = WorkflowForge.Extensions.Audit.AuditMiddleware;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Shows what each <see cref="AuditDetailLevel"/> actually captures, and the effect of
/// LogDataPayloads, by running the same workflow at every level and printing the difference.
/// </summary>
public class AuditDetailLevelsSample : ISample
{
    public string Name => "Audit Detail Levels";
    public string Description => "Side-by-side comparison of Minimal, Standard, Verbose and Complete audit entries";

    public async Task RunAsync()
    {
        Console.WriteLine("Comparing audit detail levels on the same workflow...");
        Console.WriteLine();

        foreach (var level in new[]
                 {
                     AuditDetailLevel.Minimal,
                     AuditDetailLevel.Standard,
                     AuditDetailLevel.Verbose,
                     AuditDetailLevel.Complete
                 })
        {
            await DescribeAsync(level.ToString(), new AuditMiddlewareOptions { DetailLevel = level });
        }

        await DescribeAsync(
            "Standard + LogDataPayloads",
            new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Standard, LogDataPayloads = true });
    }

    private static async Task DescribeAsync(string label, AuditMiddlewareOptions options)
    {
        var provider = new InMemoryAuditProvider();

        using var foundry = WorkflowForge.CreateFoundry("AuditDetailLevels");
        foundry.AddMiddleware(new AuditTrailMiddleware(provider, options, initiatedBy: "sample-user"));

        foundry
            .WithOperation(WorkflowOperations.Create("PrepareOrder", _ => (object?)"ORDER-4711"))
            .WithOperation(WorkflowOperations.Create("ChargeCard", input =>
            {
                return (object?)$"charged:{input}";
            }));

        foundry.SetProperty("Region", "eu-west-1");

        await foundry.ForgeAsync();

        var completed = provider.Entries.Last(e => e.EventType == AuditEventType.OperationCompleted);

        Console.WriteLine($"{label}:");
        Console.WriteLine($"   Entries:     {provider.Entries.Count}");
        Console.WriteLine($"   InitiatedBy: {completed.InitiatedBy ?? "(none)"}");
        Console.WriteLine($"   DurationMs:  {(completed.DurationMs.HasValue ? completed.DurationMs.Value.ToString() : "(none)")}");
        Console.WriteLine($"   Metadata:    {(completed.Metadata.Count == 0 ? "(empty)" : string.Join(", ", completed.Metadata.Keys))}");
        Console.WriteLine();
    }
}
