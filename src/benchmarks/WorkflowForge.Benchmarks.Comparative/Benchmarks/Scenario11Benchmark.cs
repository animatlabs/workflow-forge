using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario11Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(4, 8, 16)]
    public int OperationCount { get; set; }

    [Params(2, 4)]
    public int ConcurrencyLevel { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters
        {
            OperationCount = OperationCount,
            ConcurrencyLevel = ConcurrencyLevel
        };
        _workflowForgeScenario = new Scenario11_ParallelExecution_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario11_ParallelExecution_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(11, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(11, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(11, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(11, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Parallel Execution")]
    public async Task<ScenarioResult> WorkflowForge_ParallelExecution() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Parallel Execution")]
    public async Task<ScenarioResult> WorkflowCore_ParallelExecution() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Parallel Execution")]
    public async Task<ScenarioResult> Elsa_ParallelExecution() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Parallel Execution")]
    public async Task<ScenarioResult> Temporal_ParallelExecution() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Parallel Execution")]
    public async Task<ScenarioResult> Dapr_ParallelExecution() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Parallel Execution")]
    public async Task<ScenarioResult> WorkflowEngineNet_ParallelExecution() => await _workflowEngineNetScenario.ExecuteAsync();
}