using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario5_ConcurrentExecution_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Concurrent Execution";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent workflows";

    public Scenario5_ConcurrentExecution_Elsa(ScenarioParameters parameters)
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
        var tasks = new List<Task<RunWorkflowResult>>();

        for (int i = 0; i < _parameters.ConcurrencyLevel; i++)
        {
            var workflow = new ConcurrentWorkflow();
            tasks.Add(_workflowRunner.RunAsync(workflow));
        }

        var results = await Task.WhenAll(tasks);
        var completed = results.Count(r => r.WorkflowState.Status == WorkflowStatus.Finished);

        return new ScenarioResult
        {
            Success = completed == _parameters.ConcurrencyLevel,
            OperationsExecuted = completed * 10,
            OutputData = $"{completed} workflows completed",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class ConcurrentWorkflow : WorkflowBase
    {
        public int ExecutedCount { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities = {
                    new While(context => ExecutedCount < 10)
                    {
                        Body = new Sequence
                        {
                            Activities = { new ConcurrentActivity(this) }
                        }
                    }
                }
            };
        }
    }

    public class ConcurrentActivity : CodeActivity
    {
        private readonly ConcurrentWorkflow _workflow;

        public ConcurrentActivity(ConcurrentWorkflow workflow)
        { _workflow = workflow; }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.ExecutedCount++;
        }
    }
}