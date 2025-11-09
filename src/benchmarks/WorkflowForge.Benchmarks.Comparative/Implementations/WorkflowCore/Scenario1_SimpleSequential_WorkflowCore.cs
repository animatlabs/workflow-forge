using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

/// <summary>
/// Scenario 1: Simple Sequential Workflow - Workflow Core 3.17 Implementation
/// Tests: Basic workflow execution with N sequential operations
/// </summary>
public class Scenario1_SimpleSequential_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations";

    public Scenario1_SimpleSequential_WorkflowCore(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        // Setup Workflow Core with minimal configuration
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();

        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();

        // Register workflow
        _workflowHost.RegisterWorkflow<SimpleSequentialWorkflow, SimpleSequentialData>();
        _workflowHost.Start();

        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new SimpleSequentialData
        {
            OperationCount = _parameters.OperationCount,
            ExecutedCount = 0,
            CompletionSource = completionSource
        };

        // Start workflow
        var workflowId = await _workflowHost.StartWorkflow("SimpleSequential", data);

        // Wait for completion using event-based mechanism (no polling)
        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete,
            OperationsExecuted = data.ExecutedCount,
            OutputData = $"Completed {data.ExecutedCount} operations",
            Metadata = { ["FrameworkName"] = "WorkflowCore", ["WorkflowId"] = workflowId }
        };
    }

    public async Task CleanupAsync()
    {
        if (_workflowHost != null)
        {
            _workflowHost.Stop();
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await Task.CompletedTask;
    }

    // Workflow definition
    public class SimpleSequentialWorkflow : IWorkflow<SimpleSequentialData>
    {
        public string Id => "SimpleSequential";
        public int Version => 1;

        public void Build(IWorkflowBuilder<SimpleSequentialData> builder)
        {
            // Build a simple loop that executes N operations
            builder
                .StartWith<SimpleOperationStep>()
                .While(data => data.ExecutedCount < data.OperationCount)
                    .Do(x => x
                        .StartWith<SimpleOperationStep>())
                .Then<CompleteStep>();
        }
    }

    // Workflow data
    public class SimpleSequentialData
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    // Step that performs the operation
    public class SimpleOperationStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (SimpleSequentialData)context.Workflow.Data;
            data.ExecutedCount++;
            return ExecutionResult.Next();
        }
    }

    // Final step to mark completion
    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (SimpleSequentialData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}