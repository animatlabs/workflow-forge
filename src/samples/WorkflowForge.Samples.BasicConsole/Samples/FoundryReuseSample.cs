using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates reusing a foundry across multiple workflows.
/// </summary>
public class FoundryReuseSample : ISample
{
    public string Name => "Foundry Reuse";
    public string Description => "Shows reusing a foundry and property persistence";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating foundry reuse across workflows...");

        var smith = WorkflowForge.CreateSmith();
        using var foundry = smith.CreateFoundry();

        var workflowA = WorkflowForge.CreateWorkflow("ReuseA")
            .AddOperation(new RecordRunOperation("FirstWorkflow"))
            .Build();

        var workflowB = WorkflowForge.CreateWorkflow("ReuseB")
            .AddOperation(new RecordRunOperation("SecondWorkflow"))
            .Build();

        await smith.ForgeAsync(workflowA, foundry);
        await smith.ForgeAsync(workflowB, foundry);

        var runs = foundry.GetPropertyOrDefault<List<string>>("runs") ?? new();
        Console.WriteLine($"Runs recorded in foundry: {string.Join(", ", runs)}");
    }

    private sealed class RecordRunOperation : WorkflowOperationBase
    {
        private readonly string _label;

        public RecordRunOperation(string label) => _label = label;

        public override string Name => $"Record:{_label}";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var runs = foundry.GetPropertyOrDefault("runs", new List<string>());
            runs.Add(_label);
            foundry.SetProperty("runs", runs);
            Console.WriteLine($"Recorded run: {_label}");
            return Task.FromResult(inputData);
        }
    }
}