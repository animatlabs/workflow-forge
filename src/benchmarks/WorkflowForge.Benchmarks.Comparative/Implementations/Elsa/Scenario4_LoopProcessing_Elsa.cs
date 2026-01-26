using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario4_LoopProcessing_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Loop/ForEach Processing";
    public string Description => $"Process {_parameters.ItemCount} items in collection";

    public Scenario4_LoopProcessing_Elsa(ScenarioParameters parameters)
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
        var workflow = new LoopWorkflow { ItemCount = _parameters.ItemCount };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.ProcessedCount == _parameters.ItemCount,
            OperationsExecuted = workflow.ProcessedCount,
            OutputData = $"Processed {workflow.ProcessedCount} items",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class LoopWorkflow : WorkflowBase
    {
        public int ItemCount { get; set; }
        public int ProcessedCount { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = {
                    new While(context => ProcessedCount < ItemCount)
                    {
                        Body = new Sequence
                        {
                            Activities = { new ProcessItemActivity(this) }
                        }
                    }
                }
            };
        }
    }

    public class ProcessItemActivity : CodeActivity
    {
        private readonly LoopWorkflow _workflow;

        public ProcessItemActivity(LoopWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.ProcessedCount++;
        }
    }
}