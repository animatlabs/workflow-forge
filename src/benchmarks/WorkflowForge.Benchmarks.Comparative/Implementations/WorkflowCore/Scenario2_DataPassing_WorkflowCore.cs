using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario2_DataPassing_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Data Passing Workflow";
    public string Description => $"Read, modify, and write {_parameters.OperationCount} context values";

    public Scenario2_DataPassing_WorkflowCore(ScenarioParameters parameters)
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
        _workflowHost.RegisterWorkflow<DataPassingWorkflow, DataPassingData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new DataPassingData
        {
            OperationCount = _parameters.OperationCount,
            CompletionSource = completionSource
        };
        var workflowId = await _workflowHost.StartWorkflow("DataPassing", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.FinalValue == _parameters.OperationCount,
            OperationsExecuted = data.ExecutedCount,
            OutputData = $"Final value: {data.FinalValue}",
            Metadata = { ["FrameworkName"] = "WorkflowCore", ["WorkflowId"] = workflowId }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;
    }

    public class DataPassingWorkflow : IWorkflow<DataPassingData>
    {
        public string Id => "DataPassing";
        public int Version => 1;

        public void Build(IWorkflowBuilder<DataPassingData> builder)
        {
            builder
                .StartWith<DataOperationStep>()
                .While(data => data.ExecutedCount < data.OperationCount)
                    .Do(x => x.StartWith<DataOperationStep>())
                .Then<CompleteStep>();
        }
    }

    public class DataPassingData
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int FinalValue { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class DataOperationStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (DataPassingData)context.Workflow.Data;
            data.FinalValue++;
            data.ExecutedCount++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (DataPassingData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}