using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario11_ParallelExecution_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Parallel Execution";
    public string Description => $"Execute {_parameters.OperationCount} parallel branches";

    public Scenario11_ParallelExecution_Elsa(ScenarioParameters parameters)
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
        var workflow = new ParallelWorkflow
        {
            BranchCount = _parameters.OperationCount
        };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.Results.Count == workflow.BranchCount,
            OperationsExecuted = workflow.Results.Count,
            OutputData = $"Completed {workflow.Results.Count} parallel branches",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class ParallelWorkflow : WorkflowBase
    {
        public int BranchCount { get; set; }
        public ConcurrentBag<int> Results { get; } = new();

        protected override void Build(IWorkflowBuilder builder)
        {
            var parallel = new global::Elsa.Workflows.Activities.Parallel();
            for (int i = 0; i < BranchCount; i++)
            {
                parallel.Activities.Add(new global::Elsa.Workflows.Activities.Sequence
                {
                    Activities =
                    {
                        new ParallelBranchActivity(this)
                    }
                });
            }

            builder.Root = new global::Elsa.Workflows.Activities.Sequence
            {
                Activities = { parallel }
            };
        }
    }

    public class ParallelBranchActivity : CodeActivity
    {
        private readonly ParallelWorkflow _workflow;

        public ParallelBranchActivity(ParallelWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.Results.Add(1);
        }
    }
}