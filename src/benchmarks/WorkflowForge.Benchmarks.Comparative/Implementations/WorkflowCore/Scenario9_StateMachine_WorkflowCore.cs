using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario9_StateMachine_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "State Machine";
    public string Description => $"Execute {_parameters.OperationCount} state transitions";

    public Scenario9_StateMachine_WorkflowCore(ScenarioParameters parameters)
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
        _workflowHost.RegisterWorkflow<StateMachineWorkflow, StateMachineData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new StateMachineData
        {
            TransitionCount = _parameters.OperationCount,
            CompletionSource = completionSource
        };

        var workflowId = await _workflowHost.StartWorkflow("StateMachine", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.CurrentState == data.TransitionCount,
            OperationsExecuted = data.CurrentState,
            OutputData = $"Final state: {data.CurrentState}",
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

    public class StateMachineWorkflow : IWorkflow<StateMachineData>
    {
        public string Id => "StateMachine";
        public int Version => 1;

        public void Build(IWorkflowBuilder<StateMachineData> builder)
        {
            builder
                .StartWith<InitializeStep>()
                .While(data => data.CurrentState < data.TransitionCount)
                    .Do(x => x.StartWith<TransitionStep>())
                .Then<CompleteStep>();
        }
    }

    public class StateMachineData
    {
        public int TransitionCount { get; set; }
        public int CurrentState { get; set; }
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

    public class TransitionStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (StateMachineData)context.Workflow.Data;
            data.CurrentState++;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (StateMachineData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}
