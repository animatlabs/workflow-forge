using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario4Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(10, 50, 100)]
    public int ItemCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { ItemCount = ItemCount };
        _workflowForgeScenario = new Scenario4_LoopProcessing_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario4_LoopProcessing_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(4, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(4, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(4, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(4, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Loop Processing")]
    public async Task<ScenarioResult> WorkflowForge_LoopProcessing() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Loop Processing")]
    public async Task<ScenarioResult> WorkflowCore_LoopProcessing() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Loop Processing")]
    public async Task<ScenarioResult> Elsa_LoopProcessing() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Loop Processing")]
    public async Task<ScenarioResult> Temporal_LoopProcessing() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Loop Processing")]
    public async Task<ScenarioResult> Dapr_LoopProcessing() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Loop Processing")]
    public async Task<ScenarioResult> WorkflowEngineNet_LoopProcessing() => await _workflowEngineNetScenario.ExecuteAsync();
}