using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

/// <summary>
/// Scenario 1: Simple Sequential Workflow - Elsa 3.x Implementation
/// Tests: Basic workflow execution with N sequential operations
/// </summary>
public class Scenario1_SimpleSequential_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations";

    public Scenario1_SimpleSequential_Elsa(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        // Setup Elsa 3.x with minimal configuration
        var services = new ServiceCollection();

        // Add Elsa services
        services.AddElsa();

        _serviceProvider = services.BuildServiceProvider();
        _workflowRunner = _serviceProvider.GetRequiredService<IWorkflowRunner>();

        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // Create a simple workflow
        var workflow = new SimpleSequentialWorkflow
        {
            OperationCount = _parameters.OperationCount
        };

        // Run the workflow
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished,
            OperationsExecuted = workflow.ExecutedCount,
            OutputData = $"Completed {workflow.ExecutedCount} operations",
            Metadata = {
                ["FrameworkName"] = "Elsa",
                ["WorkflowInstanceId"] = result.WorkflowState.Id
            }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Simple workflow that executes N operations sequentially
    /// </summary>
    public class SimpleSequentialWorkflow : WorkflowBase
    {
        public int OperationCount { get; set; }
        public int ExecutedCount { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            // Create a sequence of N operations
            builder.Root = new Sequence
            {
                Activities =
                {
                    new While(context => ExecutedCount < OperationCount)
                    {
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new SimpleOperationActivity(this)
                            }
                        }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Simple operation activity
    /// </summary>
    public class SimpleOperationActivity : CodeActivity
    {
        private readonly SimpleSequentialWorkflow _workflow;

        public SimpleOperationActivity(SimpleSequentialWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            // Increment counter
            _workflow.ExecutedCount++;
        }
    }
}