using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario6_ErrorHandling_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowHost _workflowHost = null!;

    public string Name => "Error Handling";
    public string Description => "Handle exceptions with compensation";

    public Scenario6_ErrorHandling_WorkflowCore(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();
        _workflowHost.RegisterWorkflow<ErrorWorkflow, ErrorData>();
        _workflowHost.Start();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var completionSource = new TaskCompletionSource<bool>();
        var data = new ErrorData { CompletionSource = completionSource };
        var workflowId = await _workflowHost.StartWorkflow("ErrorHandling", data);

        var completedInTime = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == completionSource.Task;

        return new ScenarioResult
        {
            Success = completedInTime && data.IsComplete && data.Compensated,
            OperationsExecuted = 1,
            OutputData = "Error handled with compensation",
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

    public class ErrorWorkflow : IWorkflow<ErrorData>
    {
        public string Id => "ErrorHandling";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ErrorData> builder)
        {
            builder.StartWith<ErrorStep>()
                .OnError(WorkflowErrorHandling.Retry, TimeSpan.Zero)
                .Then<CompensateStep>()
                .Then<CompleteStep>();
        }
    }

    public class ErrorData
    {
        public bool ErrorThrown { get; set; }
        public bool Compensated { get; set; }
        public bool IsComplete { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
    }

    public class ErrorStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ErrorData)context.Workflow.Data;
            if (!data.ErrorThrown)
            {
                data.ErrorThrown = true;
                throw new InvalidOperationException("Benchmark error");
            }
            return ExecutionResult.Next();
        }
    }

    public class CompensateStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            ((ErrorData)context.Workflow.Data).Compensated = true;
            return ExecutionResult.Next();
        }
    }

    public class CompleteStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (ErrorData)context.Workflow.Data;
            data.IsComplete = true;
            data.CompletionSource?.TrySetResult(true);
            return ExecutionResult.Next();
        }
    }
}
