using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;

public class Scenario7_CreationOverhead_WorkflowCore : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead";
    public string Description => "Measure workflow definition registration time";

    public Scenario7_CreationOverhead_WorkflowCore(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        var serviceProvider = services.BuildServiceProvider();
        var workflowHost = serviceProvider.GetRequiredService<IWorkflowHost>();

        // Just register, don't start
        workflowHost.RegisterWorkflow<CreationWorkflow, CreationData>();

        if (serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 0,
            OutputData = "Workflow registered",
            Metadata = { ["FrameworkName"] = "WorkflowCore" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    public class CreationWorkflow : IWorkflow<CreationData>
    {
        public string Id => "CreationDefinition";
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