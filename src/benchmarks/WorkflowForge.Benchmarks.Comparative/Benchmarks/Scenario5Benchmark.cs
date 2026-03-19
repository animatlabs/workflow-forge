using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario5Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(1, 4, 8)]
    public int ConcurrencyLevel { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { ConcurrencyLevel = ConcurrencyLevel };
        _workflowForgeScenario = new Scenario5_ConcurrentExecution_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario5_ConcurrentExecution_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(5, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(5, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(5, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(5, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Concurrent Execution")]
    public async Task<ScenarioResult> WorkflowForge_ConcurrentExecution() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Concurrent Execution")]
    public async Task<ScenarioResult> WorkflowCore_ConcurrentExecution() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Concurrent Execution")]
    public async Task<ScenarioResult> Elsa_ConcurrentExecution() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Concurrent Execution")]
    public async Task<ScenarioResult> Temporal_ConcurrentExecution() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Concurrent Execution")]
    public async Task<ScenarioResult> Dapr_ConcurrentExecution() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Concurrent Execution")]
    public async Task<ScenarioResult> WorkflowEngineNet_ConcurrentExecution() => await _workflowEngineNetScenario.ExecuteAsync();
}