using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario10_LongRunning_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Long Running";
    public string Description => $"Execute {_parameters.OperationCount} delayed operations";

    public Scenario10_LongRunning_WorkflowCore(ScenarioParameters parameters)
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
        _workflowHost.RegisterWorkflow<LongRunningWorkflow, LongRunningData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new LongRunningData
        {
            OperationCount = _parameters.OperationCount,
            DelayMilliseconds = _parameters.DelayMilliseconds,
            CompletionSource = completionSource
        };

        var workflowId = await _workflowHost.StartWorkflow("LongRunning", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(10))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.ExecutedCount == data.OperationCount,
            OperationsExecuted = data.ExecutedCount,
            OutputData = $"Completed {data.ExecutedCount} delayed operations",
            Metadata = { ["FrameworkName"] = "WorkflowCore", ["WorkflowId"] = workflowId }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        await Task.CompletedTask;
    }

    public class LongRunningWorkflow : IWorkflow<LongRunningData>
    {
        public string Id => "LongRunning";
        public int Version => 1;

        public void Build(IWorkflowBuilder<LongRunningData> builder)
        {
            builder
                .StartWith<InitializeStep>()
                .While(data => data.ExecutedCount < data.OperationCount)
                    .Do(x => x.StartWith<DelayStep>())
                .Then<CompleteStep>();
        }
    }

    public class LongRunningData
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int DelayMilliseconds { get; set; }
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

    public class DelayStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = (LongRunningData)context.Workflow.Data;
            await Task.Delay(TimeSpan.FromMilliseconds(data.DelayMilliseconds));
            data.ExecutedCount++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (LongRunningData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}
