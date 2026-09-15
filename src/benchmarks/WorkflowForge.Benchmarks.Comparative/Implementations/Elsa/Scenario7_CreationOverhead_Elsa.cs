#if !NET48
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario7_CreationOverhead_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ServiceProvider? _serviceProvider;

    public string Name => "Creation Overhead";
    public string Description => "Measure workflow definition creation time";

    public Scenario7_CreationOverhead_Elsa(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        // Container construction belongs in setup. Building the whole AddElsa() graph inside the
        // timed method measured DI registration, not workflow creation, and made this arm
        // incomparable with the WorkflowCore and WorkflowForge arms.
        var services = new ServiceCollection();
        services.AddElsa();
        _serviceProvider = services.BuildServiceProvider();
        _ = _serviceProvider.GetRequiredService<IWorkflowRunner>();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // Just create the workflow definition, don't execute it.
        var workflow = new CreationWorkflow();
        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = workflow != null,
            OperationsExecuted = 0,
            OutputData = "Workflow created with 10 activities",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        return Task.CompletedTask;
    }

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
#endif