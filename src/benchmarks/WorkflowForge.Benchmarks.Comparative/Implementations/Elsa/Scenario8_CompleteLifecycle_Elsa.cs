#if !NET48
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario8_CompleteLifecycle_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ServiceProvider? _serviceProvider;
    private IWorkflowRunner? _workflowRunner;

    public string Name => "Complete Lifecycle";
    public string Description => "Full create→execute→cleanup cycle";

    public Scenario8_CompleteLifecycle_Elsa(ScenarioParameters parameters)
    { _parameters = parameters; }

    public Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        _serviceProvider = services.BuildServiceProvider();
        _workflowRunner = _serviceProvider.GetRequiredService<IWorkflowRunner>();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var workflow = new LifecycleWorkflow();
        var result = await _workflowRunner!.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.Executed,
            OperationsExecuted = 1,
            OutputData = "Lifecycle complete",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        _serviceProvider = null;
        _workflowRunner = null;
        return Task.CompletedTask;
    }

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
#endif