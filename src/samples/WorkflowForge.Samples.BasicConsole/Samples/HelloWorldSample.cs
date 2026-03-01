using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// The simplest possible WorkflowForge example.
/// Demonstrates basic workflow creation, foundry creation, and execution with smith.
/// </summary>
public class HelloWorldSample : ISample
{
    public string Name => "Hello World";
    public string Description => "The simplest WorkflowForge workflow with a single greeting operation";

    public async Task RunAsync()
    {
        Console.WriteLine("Running the simplest WorkflowForge workflow...");

        // Step 1: Create a workflow using WorkflowBuilder
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("HelloWorldWorkflow")
            .WithDescription("A simple greeting workflow")
            .AddOperation(new GreetingOperation("Hello from WorkflowForge!"))
            .Build();

        // Step 2: Create a foundry for execution context
        using var foundry = WorkflowForge.CreateFoundry("HelloWorldWorkflow");

        // Step 3: Create a smith to execute the workflow
        using var smith = WorkflowForge.CreateSmith();

        // Step 4: Execute the workflow
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine("Hello World workflow completed successfully!");
        Console.WriteLine($"Result: {foundry.Properties.Count} properties in foundry");
    }
}

/// <summary>
/// A simple operation that displays a greeting message.
/// Demonstrates the basic structure of a WorkflowForge operation.
/// </summary>
public class GreetingOperation : WorkflowOperationBase
{
    private readonly string _message;

    public GreetingOperation(string message)
    {
        _message = message;
    }

    public override string Name => "GreetingOperation";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing greeting operation with message: {Message}", _message);

        // Simulate some work
        await Task.Delay(100, cancellationToken);

        // Display the greeting
        Console.WriteLine($"   {_message}");

        return _message;
    }
}