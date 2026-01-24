using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario12_EventDriven_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Event-Driven";
    public string Description => "Wait for external event then continue";

    public Scenario12_EventDriven_WorkflowCore(ScenarioParameters parameters)
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
        _workflowHost.RegisterWorkflow<EventDrivenWorkflow, EventDrivenData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new EventDrivenData
        {
            EventGate = new ManualResetEventSlim(false),
            CompletionSource = completionSource
        };

        var workflowId = await _workflowHost.StartWorkflow("EventDriven", data);

        _ = Task.Run(() =>
        {
            Thread.Sleep(_parameters.DelayMilliseconds);
            data.EventGate.Set();
        });

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.EventReceived,
            OperationsExecuted = data.EventReceived ? 2 : 1,
            OutputData = data.EventReceived ? "Event handled" : "Event timed out",
            Metadata = { ["FrameworkName"] = "WorkflowCore", ["WorkflowId"] = workflowId }
        };
    }

    public async Task CleanupAsync()
    {
        _workflowHost?.Stop();
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;
    }

    public class EventDrivenWorkflow : IWorkflow<EventDrivenData>
    {
        public string Id => "EventDriven";
        public int Version => 1;

        public void Build(IWorkflowBuilder<EventDrivenData> builder)
        {
            builder
                .StartWith<WaitForEventStep>()
                .Then<HandleEventStep>()
                .Then<CompleteStep>();
        }
    }

    public class EventDrivenData
    {
        public ManualResetEventSlim EventGate { get; set; } = new(false);
        public bool EventReceived { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class WaitForEventStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (EventDrivenData)context.Workflow.Data;
            data.EventReceived = data.EventGate.Wait(TimeSpan.FromSeconds(1));
            return ExecutionResult.Next();
        }
    }

    public class HandleEventStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (EventDrivenData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}
