using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario3_ConditionalBranching_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Conditional Branching";
    public string Description => $"Execute {_parameters.OperationCount} conditional operations (50/50 true/false)";

    public Scenario3_ConditionalBranching_WorkflowCore(ScenarioParameters parameters)
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
        _workflowHost.RegisterWorkflow<ConditionalWorkflow, ConditionalData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new ConditionalData { CompletionSource = completionSource, OperationCount = _parameters.OperationCount };
        var workflowId = await _workflowHost.StartWorkflow("Conditional", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete,
            OperationsExecuted = data.TrueCount + data.FalseCount,
            OutputData = $"True: {data.TrueCount}, False: {data.FalseCount}",
            Metadata = { ["FrameworkName"] = "WorkflowCore" }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        await Task.CompletedTask;
    }

    public class ConditionalWorkflow : IWorkflow<ConditionalData>
    {
        public string Id => "Conditional";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ConditionalData> builder)
        {
            builder
                .StartWith<ConditionalStep>()
                .While(data => data.ExecutedCount < data.OperationCount)
                    .Do(x => x.StartWith<ConditionalStep>())
                .Then<CompleteStep>();
        }
    }

    public class ConditionalData
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int TrueCount { get; set; }
        public int FalseCount { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class ConditionalStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ConditionalData)context.Workflow.Data;
            if (data.ExecutedCount % 2 == 0)
                data.TrueCount++;
            else
                data.FalseCount++;
            data.ExecutedCount++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ConditionalData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}
