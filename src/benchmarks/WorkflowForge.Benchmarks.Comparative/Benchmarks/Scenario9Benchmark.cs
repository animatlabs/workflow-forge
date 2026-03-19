using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario9Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(5, 10, 25)]
    public int TransitionCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = TransitionCount };
        _workflowForgeScenario = new Scenario9_StateMachine_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario9_StateMachine_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(9, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(9, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(9, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(9, parameters);
        _workflowEngineNetScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
        _temporalScenario.CleanupAsync().GetAwaiter().GetResult();
        _daprScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - State Machine")]
    public async Task<ScenarioResult> WorkflowForge_StateMachine() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - State Machine")]
    public async Task<ScenarioResult> WorkflowCore_StateMachine() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - State Machine")]
    public async Task<ScenarioResult> Elsa_StateMachine() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - State Machine")]
    public async Task<ScenarioResult> Temporal_StateMachine() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - State Machine")]
    public async Task<ScenarioResult> Dapr_StateMachine() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - State Machine")]
    public async Task<ScenarioResult> WorkflowEngineNet_StateMachine() => await _workflowEngineNetScenario.ExecuteAsync();
}