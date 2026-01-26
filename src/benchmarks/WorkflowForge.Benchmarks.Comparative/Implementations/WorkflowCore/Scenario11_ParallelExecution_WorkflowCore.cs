using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario11_ParallelExecution_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Parallel Execution";
    public string Description => $"Execute {_parameters.OperationCount} parallel branches";

    public Scenario11_ParallelExecution_WorkflowCore(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();
        _workflowHost.RegisterWorkflow<ParallelWorkflow, ParallelData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new ParallelData
        {
            Items = Enumerable.Range(0, _parameters.OperationCount).ToList(),
            CompletionSource = completionSource
        };

        var workflowId = await _workflowHost.StartWorkflow("ParallelExecution", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.ExecutedCount == _parameters.OperationCount,
            OperationsExecuted = data.ExecutedCount,
            OutputData = $"Completed {data.ExecutedCount} parallel branches",
            Metadata = { ["FrameworkName"] = "WorkflowCore", ["WorkflowId"] = workflowId }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;
    }

    public class ParallelWorkflow : IWorkflow<ParallelData>
    {
        public string Id => "ParallelExecution";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ParallelData> builder)
        {
            builder
                .StartWith<InitializeStep>()
                .ForEach(data => data.Items)
                    .Do(x => x.StartWith<ParallelStep>())
                .Then<CompleteStep>();
        }
    }

    public class ParallelData
    {
        public List<int> Items { get; set; } = new();
        public int ExecutedCount;
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class InitializeStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Next();
        }
    }

    public class ParallelStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ParallelData)context.Workflow.Data;
            Interlocked.Increment(ref data.ExecutedCount);
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ParallelData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}