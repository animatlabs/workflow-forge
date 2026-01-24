using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario7_CreationOverhead_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead";
    public string Description => "Measure workflow definition creation time";

    public Scenario7_CreationOverhead_Elsa(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        var serviceProvider = services.BuildServiceProvider();
        var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

        // Just create workflow instance, don't execute
        var workflow = new CreationWorkflow();

        if (serviceProvider is IDisposable disposable) disposable.Dispose();
        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = workflow != null,
            OperationsExecuted = 0,
            OutputData = "Workflow created with 10 activities",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    public class CreationWorkflow : WorkflowBase
    {
        private const int ActivityCount = 10;

        protected override void Build(IWorkflowBuilder builder)
        {
            var sequence = new Sequence();
            for (int i = 0; i < ActivityCount; i++)
            {
                sequence.Activities.Add(new NoOpActivity());
            }

            builder.Root = sequence;
        }
    }

    public class NoOpActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }
}