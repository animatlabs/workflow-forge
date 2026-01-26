using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario3_ConditionalBranching_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Conditional Branching";
    public string Description => $"Execute {_parameters.OperationCount} conditional operations (50/50 true/false)";

    public Scenario3_ConditionalBranching_Elsa(ScenarioParameters parameters)
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
        var workflow = new ConditionalWorkflow { OperationCount = _parameters.OperationCount };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished,
            OperationsExecuted = workflow.TrueCount + workflow.FalseCount,
            OutputData = $"True: {workflow.TrueCount}, False: {workflow.FalseCount}",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class ConditionalWorkflow : WorkflowBase
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int TrueCount { get; set; }
        public int FalseCount { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = {
                    new While(context => ExecutedCount < OperationCount)
                    {
                        Body = new Sequence
                        {
                            Activities = { new ConditionalActivity(this) }
                        }
                    }
                }
            };
        }
    }

    public class ConditionalActivity : CodeActivity
    {
        private readonly ConditionalWorkflow _workflow;

        public ConditionalActivity(ConditionalWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            if (_workflow.ExecutedCount % 2 == 0)
                _workflow.TrueCount++;
            else
                _workflow.FalseCount++;
            _workflow.ExecutedCount++;
        }
    }
}