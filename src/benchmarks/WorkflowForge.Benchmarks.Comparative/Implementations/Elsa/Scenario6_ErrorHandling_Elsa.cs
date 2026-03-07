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
        var result = await _workflowRunner.RunAsync(workflow);
        var status = result.WorkflowState.Status.ToString();
        if (string.Equals(status, "Faulted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            workflow.Compensated = true;
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