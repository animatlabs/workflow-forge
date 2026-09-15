#if !NET48
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario6_ErrorHandling_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Error Handling";
    public string Description => "Handle exceptions with compensation";

    public Scenario6_ErrorHandling_Elsa(ScenarioParameters parameters)
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
        var workflow = new ErrorHandlingWorkflow();

        // Elsa surfaces an activity fault in more than one way depending on the configured
        // incident strategy: a recorded incident, a Faulted sub-status, or a thrown exception.
        // Treat all three as the error path, otherwise this arm silently measures a no-op.
        try
        {
            var result = await _workflowRunner.RunAsync(workflow);
            var state = result.WorkflowState;

            if (state.Incidents.Count > 0
                || state.Status.ToString().IndexOf("Fault", StringComparison.OrdinalIgnoreCase) >= 0
                || state.SubStatus.ToString().IndexOf("Fault", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                workflow.Compensated = true;
            }
        }
        catch (InvalidOperationException)
        {
            workflow.Compensated = true;
        }

        if (!workflow.ErrorThrown)
        {
            throw new InvalidOperationException(
                "Scenario6 Elsa arm never executed the failing activity; it is not measuring error handling.");
        }

        return new ScenarioResult
        {
            Success = workflow.Compensated,
            OperationsExecuted = 1,
            OutputData = "Error handled with compensation",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class ErrorHandlingWorkflow : WorkflowBase
    {
        public bool ErrorThrown { get; set; }
        public bool Compensated { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = {
                    new ErrorActivity(this)
                }
            };
        }
    }

    public class ErrorActivity : CodeActivity
    {
        private readonly ErrorHandlingWorkflow _workflow;

        public ErrorActivity(ErrorHandlingWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            if (!_workflow.ErrorThrown)
            {
                _workflow.ErrorThrown = true;
                throw new InvalidOperationException("Benchmark error");
            }
        }
    }
}
#endif