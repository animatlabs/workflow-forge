using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario3Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(10, 25, 50)]
    public int OperationCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = OperationCount };
        _workflowForgeScenario = new Scenario3_ConditionalBranching_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario3_ConditionalBranching_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(3, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(3, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(3, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(3, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Conditional Branching")]
    public async Task<ScenarioResult> WorkflowForge_ConditionalBranching() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Conditional Branching")]
    public async Task<ScenarioResult> WorkflowCore_ConditionalBranching() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Conditional Branching")]
    public async Task<ScenarioResult> Elsa_ConditionalBranching() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Conditional Branching")]
    public async Task<ScenarioResult> Temporal_ConditionalBranching() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Conditional Branching")]
    public async Task<ScenarioResult> Dapr_ConditionalBranching() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Conditional Branching")]
    public async Task<ScenarioResult> WorkflowEngineNet_ConditionalBranching() => await _workflowEngineNetScenario.ExecuteAsync();
}