using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario7_CreationOverhead_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ServiceProvider? _serviceProvider;
    private IWorkflowHost? _workflowHost;

    public string Name => "Creation Overhead";
    public string Description => "Measure workflow definition registration time";

    public Scenario7_CreationOverhead_WorkflowCore(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        _serviceProvider = services.BuildServiceProvider();
        _workflowHost = _serviceProvider.GetRequiredService<IWorkflowHost>();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // WorkflowCore's registry throws InvalidOperationException on a duplicate (Id, Version)
        // registration, so a benchmark strategy that invokes this more than once per SetupAsync
        // needs a distinct Id per call to keep measuring registration, not the exception path.
        var id = "CreationDefinition_" + Guid.NewGuid().ToString("N");
        _workflowHost!.Registry.RegisterWorkflow(new CreationWorkflow(id));
        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 0,
            OutputData = "Workflow registered",
            Metadata = { ["FrameworkName"] = "WorkflowCore" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        _serviceProvider = null;
        _workflowHost = null;
        return Task.CompletedTask;
    }

    public class CreationWorkflow : IWorkflow<CreationData>
    {
        public CreationWorkflow(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public int Version => 1;

        public void Build(IWorkflowBuilder<CreationData> builder)
        {
            builder.StartWith<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>()
                .Then<NoOpStep>();
        }
    }

    public class CreationData
    {
    }

    public class NoOpStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Next();
        }
    }
}
