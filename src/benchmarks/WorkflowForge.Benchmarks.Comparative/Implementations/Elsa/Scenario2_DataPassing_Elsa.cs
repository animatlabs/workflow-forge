using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario2_DataPassing_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Data Passing Workflow";
    public string Description => $"Read, modify, and write {_parameters.OperationCount} context values";

    public Scenario2_DataPassing_Elsa(ScenarioParameters parameters)
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
        var workflow = new DataPassingWorkflow { OperationCount = _parameters.OperationCount };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished,
            OperationsExecuted = workflow.ExecutedCount,
            OutputData = $"Final value: {workflow.FinalValue}",
            Metadata = { ["FrameworkName"] = "Elsa", ["WorkflowInstanceId"] = result.WorkflowState.Id }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class DataPassingWorkflow : WorkflowBase
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }
        public int FinalValue { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = {
                    new While(context => ExecutedCount < OperationCount)
                    {
                        Body = new Sequence
                        {
                            Activities = { new DataOperationActivity(this) }
                        }
                    }
                }
            };
        }
    }

    public class DataOperationActivity : CodeActivity
    {
        private readonly DataPassingWorkflow _workflow;

        public DataOperationActivity(DataPassingWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.FinalValue++;
            _workflow.ExecutedCount++;
        }
    }
}