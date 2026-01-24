using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario10_LongRunning_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Long Running";
    public string Description => $"Execute {_parameters.OperationCount} delayed operations";

    public Scenario10_LongRunning_Elsa(ScenarioParameters parameters)
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
        var workflow = new LongRunningWorkflow
        {
            OperationCount = _parameters.OperationCount,
            DelayMilliseconds = _parameters.DelayMilliseconds
        };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.ExecutedCount == workflow.OperationCount,
            OperationsExecuted = workflow.ExecutedCount,
            OutputData = $"Completed {workflow.ExecutedCount} delayed operations",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class LongRunningWorkflow : WorkflowBase
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int DelayMilliseconds { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new global::Elsa.Workflows.Activities.Sequence
            {
                Activities =
                {
                    new While(context => ExecutedCount < OperationCount)
                    {
                        Body = new global::Elsa.Workflows.Activities.Sequence
                        {
                            Activities =
                            {
                                new DelayActivity(DelayMilliseconds),
                                new IncrementActivity(this)
                            }
                        }
                    }
                }
            };
        }
    }

    public class DelayActivity : CodeActivity
    {
        private readonly int _delayMilliseconds;

        public DelayActivity(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            Task.Delay(_delayMilliseconds).GetAwaiter().GetResult();
        }
    }

    public class IncrementActivity : CodeActivity
    {
        private readonly LongRunningWorkflow _workflow;

        public IncrementActivity(LongRunningWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.ExecutedCount++;
        }
    }
}
