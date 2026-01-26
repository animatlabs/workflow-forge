using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario8_CompleteLifecycle_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Complete Lifecycle";
    public string Description => "Full create→execute→cleanup cycle";

    public Scenario8_CompleteLifecycle_Elsa(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // Create
        var services = new ServiceCollection();
        services.AddElsa();
        var serviceProvider = services.BuildServiceProvider();
        var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

        // Execute
        var workflow = new LifecycleWorkflow();
        var result = await workflowRunner.RunAsync(workflow);

        // Cleanup
        if (serviceProvider is IDisposable disposable) disposable.Dispose();

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.Executed,
            OperationsExecuted = 1,
            OutputData = "Lifecycle complete",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    public class LifecycleWorkflow : WorkflowBase
    {
        public bool Executed { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = { new ExecuteActivity(this) }
            };
        }
    }

    public class ExecuteActivity : CodeActivity
    {
        private readonly LifecycleWorkflow _workflow;

        public ExecuteActivity(LifecycleWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.Executed = true;
        }
    }
}