using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;

public class Scenario9_StateMachine_Elsa : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private IServiceProvider _serviceProvider = null!;
    private IWorkflowRunner _workflowRunner = null!;

    public string Name => "State Machine";
    public string Description => $"Execute {_parameters.OperationCount} state transitions";

    public Scenario9_StateMachine_Elsa(ScenarioParameters parameters)
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
        var workflow = new StateMachineWorkflow
        {
            TransitionCount = _parameters.OperationCount
        };
        var result = await _workflowRunner.RunAsync(workflow);

        return new ScenarioResult
        {
            Success = result.WorkflowState.Status == WorkflowStatus.Finished && workflow.CurrentState == workflow.TransitionCount,
            OperationsExecuted = workflow.CurrentState,
            OutputData = $"Final state: {workflow.CurrentState}",
            Metadata = { ["FrameworkName"] = "Elsa" }
        };
    }

    public Task CleanupAsync()
    {
        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        return Task.CompletedTask;
    }

    public class StateMachineWorkflow : WorkflowBase
    {
        public int TransitionCount { get; set; }
        public int CurrentState { get; set; }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities =
                {
                    new While(context => CurrentState < TransitionCount)
                    {
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new TransitionActivity(this)
                            }
                        }
                    }
                }
            };
        }
    }

    public class TransitionActivity : CodeActivity
    {
        private readonly StateMachineWorkflow _workflow;

        public TransitionActivity(StateMachineWorkflow workflow)
        {
            _workflow = workflow;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
            _workflow.CurrentState++;
        }
    }
}