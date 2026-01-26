using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario12_EventDriven_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Event-Driven";
    public string Description => "Wait for external event then continue";

    public Scenario12_EventDriven_Elsa(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

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
        var workflow = new EventDrivenWorkflow
        {
            DelayMilliseconds = _parameters.DelayMilliseconds
        };

        _ = Task.Run(() =>
        {
            Thread.Sleep(_parameters.DelayMilliseconds);
            workflow.EventGate.Set();
        });

        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.EventReceived,
            OperationsExecuted = workflow.EventReceived ? 2 : 1,
            OutputData = workflow.EventReceived ? "Event handled" : "Event timed out",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class EventDrivenWorkflow : WorkflowBase
    {
        public int DelayMilliseconds { get; set; }
        public ManualResetEventSlim EventGate { get; } = new(false);
        public bool EventReceived { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities =
                {
                    new WaitForEventActivity(this),
                    new HandleEventActivity(this)
                }
            };
        }
    }

    public class WaitForEventActivity : CodeActivity
    {
        private readonly EventDrivenWorkflow _workflow;

        public WaitForEventActivity(EventDrivenWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.EventReceived = _workflow.EventGate.Wait(TimeSpan.FromSeconds(1));
        }
    }

    public class HandleEventActivity : CodeActivity
    {
        private readonly EventDrivenWorkflow _workflow;

        public HandleEventActivity(EventDrivenWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            if (!_workflow.EventReceived)
            {
                return;
            }
        }
    }
}