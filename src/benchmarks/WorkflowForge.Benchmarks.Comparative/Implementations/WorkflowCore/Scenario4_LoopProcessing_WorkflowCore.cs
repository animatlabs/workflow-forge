using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario4_LoopProcessing_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Loop/ForEach Processing";
    public string Description => $"Process {_parameters.ItemCount} items in collection";

    public Scenario4_LoopProcessing_WorkflowCore(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();
        _workflowHost.RegisterWorkflow<LoopWorkflow, LoopData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new LoopData
        {
            ItemCount = _parameters.ItemCount,
            CompletionSource = completionSource
        };
        var workflowId = await _workflowHost.StartWorkflow("LoopProcessing", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(10))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.ProcessedCount == _parameters.ItemCount,
            OperationsExecuted = data.ProcessedCount,
            OutputData = $"Processed {data.ProcessedCount} items",
            Metadata = { ["FrameworkName"] = "WorkflowCore" }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;
    }

    public class LoopWorkflow : IWorkflow<LoopData>
    {
        public string Id => "LoopProcessing";
        public int Version => 1;

        public void Build(IWorkflowBuilder<LoopData> builder)
        {
            builder.StartWith<ProcessItemStep>()
                .While(data => data.ProcessedCount < data.ItemCount)
                    .Do(x => x.StartWith<ProcessItemStep>())
                .Then<CompleteStep>();
        }
    }

    public class LoopData
    {
        public int ItemCount { get; set; }
        public int ProcessedCount { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class ProcessItemStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (LoopData)context.Workflow.Data;
            data.ProcessedCount++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (LoopData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}