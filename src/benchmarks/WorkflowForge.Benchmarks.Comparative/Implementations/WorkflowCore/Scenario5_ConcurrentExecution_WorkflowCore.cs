using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario5_ConcurrentExecution_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Concurrent Execution";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent workflows";

    public Scenario5_ConcurrentExecution_WorkflowCore(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();
        _workflowHost.RegisterWorkflow<ConcurrentWorkflow, ConcurrentData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var workflowIds = new List<string>();
        var dataList = new List<ConcurrentData>();
        var completionTasks = new List<Task>();

        for (int i = 0; i < _parameters.ConcurrencyLevel; i++)
        {
            var completionSource = new TaskCompletionSource<bool>();
            var data = new ConcurrentData { CompletionSource = completionSource };
            dataList.Add(data);
            completionTasks.Add(completionSource.Task);
            var id = await _workflowHost.StartWorkflow("Concurrent", data);
            workflowIds.Add(id);
        }

        // Wait for all workflows to complete or timeout
        var allCompleted = await Task.WhenAny(
            Task.WhenAll(completionTasks),
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == Task.WhenAll(completionTasks);

        var completed = dataList.Count(d => d.IsComplete);
        return new ScenarioResult
        {
            Success = allCompleted && completed == _parameters.ConcurrencyLevel,
            OperationsExecuted = completed * 10,
            OutputData = $"{completed} workflows completed",
            Metadata = { ["FrameworkName"] = "WorkflowCore" }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;
    }

    public class ConcurrentWorkflow : IWorkflow<ConcurrentData>
    {
        public string Id => "Concurrent";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ConcurrentData> builder)
        {
            builder.StartWith<ConcurrentStep>()
                .While(data => data.ExecutedCount < 10)
                    .Do(x => x.StartWith<ConcurrentStep>())
                .Then<CompleteStep>();
        }
    }

    public class ConcurrentData
    {
        public int ExecutedCount { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class ConcurrentStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            ((ConcurrentData)context.Workflow.Data).ExecutedCount++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ConcurrentData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}